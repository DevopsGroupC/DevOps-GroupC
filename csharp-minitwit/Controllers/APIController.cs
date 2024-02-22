using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using csharp_minitwit.Models;
using csharp_minitwit.Services;
using csharp_minitwit.Utils;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Net.Http;
using System.Text.Json.Nodes;


namespace csharp_minitwit.Controllers;


[Route("api")]
[ApiController]
public class APIController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IConfiguration _configuration;
    private readonly string _perPage;
    private readonly PasswordHasher<UserModel> _passwordHasher;

    public APIController(IDatabaseService databaseService, IConfiguration configuration)
    {
        _databaseService = databaseService;
        _configuration = configuration;
        _perPage = configuration.GetValue<string>("Constants:PerPage")!;
        _passwordHasher = new PasswordHasher<UserModel>();
    }



    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("Test");
    }

    public APIErrorResponse NotReqFromSimulator(HttpRequest request)

    {
        var fromSimulator = request.Headers["Authorization"].ToString();
        Console.WriteLine(fromSimulator);
        if (fromSimulator != "Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh")
        {
            return new APIErrorResponse
            {
                ErrorMessage = "You are not authorized to use this resource!",
                ErrorCode = 403
            };
        }
        return null;
    }

    public int GetUserID(string username)
    {
        var sqlQuery = "SELECT user_id FROM user WHERE username = @Username";
        var parameters = new Dictionary<string, object> { { "@Username", username } };
        var result = _databaseService.QueryDb<int>(sqlQuery, parameters);



        if (result.Result.Count() == 0)
        {
            return -1;
        }
        return result.Result.FirstOrDefault();
    }

    [HttpGet("latest")]
    public IActionResult GetLatest()
    {
        var latestProcessedCommandID = 0;
        try
        {
            var latest = System.IO.File.ReadAllText("Services/latest_processed_sim_action_id.txt");
            latestProcessedCommandID = int.Parse(latest);
        }
        catch (Exception)
        {
            latestProcessedCommandID = -1;
        }
        return Ok(new { latest = latestProcessedCommandID });
    }

    public void updateLatest(string latest)
    {
        int parsedLatest = -1;
        try
        {
            parsedLatest = int.Parse(latest);
        }
        catch (System.Exception)
        {
        }
        if (parsedLatest != -1)
        {
            System.IO.File.WriteAllText("Services/latest_processed_sim_action_id.txt", parsedLatest.ToString());
        }
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// 
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] APIRegisterModel model, string latest)
    {

        updateLatest(latest);

        if (ModelState.IsValid)
        {
            //Validate form inputs
            if (string.IsNullOrEmpty(model.username))
            {
                ModelState.AddModelError("Username", "You have to enter a username");
            }
            else if (string.IsNullOrEmpty(model.pwd))
            {
                ModelState.AddModelError("Password", "You have to enter a password");
            }
            else if (string.IsNullOrEmpty(model.email))
            {
                ModelState.AddModelError("Email", "You have to enter a valid email address");
            }
            else if (await UserHelper.IsUsernameTaken(_databaseService, model.username))
            {
                ModelState.AddModelError("Username", "The username is already taken");
            }
            else
            {
                //Insert user into database
                var result = await UserHelper.InsertUser(_passwordHasher, _databaseService, model.username, model.email, model.pwd);
                //TempData["SuccessMessage"] = "You were successfully registered and can login now"; //Not needed? for the frontEnd?
                return NoContent();
            }
        }
        return BadRequest(ModelState);
    }


    /// <summary>
    /// Registers a new message for the user.
    /// </summary>
    [HttpGet("msgs")]
    public async Task<IActionResult> Messages(string latest, int no)
    {
        // Update latest
        updateLatest(latest);

        // Check if request is from simulator
        var notFromSimResponse = NotReqFromSimulator(Request);
        if (notFromSimResponse.ErrorMessage != null)
            return Forbid(notFromSimResponse.ErrorMessage);

        var sqlQuery = @"
            SELECT message.*, user.*
            FROM message
            INNER JOIN user ON message.author_id = user.user_id
            WHERE message.flagged = 0
            ORDER BY message.pub_date DESC
            LIMIT @PerPage";

        var dict = new Dictionary<string, object> { { "@PerPage", no } };
        var queryResult = await _databaseService.QueryDb<dynamic>(sqlQuery, dict);

        Console.WriteLine("queryres" + queryResult.Count());
        var filteredMsgs = queryResult.Select(msg =>
        {
            var dictB = (IDictionary<string, object>)msg;
            return new APIMessageModel
            {
                content = (string)dictB["text"],
                pub_date = (long)dictB["pub_date"],
                user = (string)dictB["username"]
            };
        }).ToList();
        Console.WriteLine(filteredMsgs);
        return Ok(filteredMsgs);
    }

    [HttpPost("msgs/{username}")]
    public async Task<IActionResult> PostMessagesPerUser(string username, [FromBody] APIMessageModel model, int no, string latest)
    {
        // Update latest
        updateLatest(latest);

        // Check if request is from simulator
        var notFromSimResponse = NotReqFromSimulator(Request);
        if (notFromSimResponse.ErrorMessage != null)
            return Forbid(notFromSimResponse.ErrorMessage);


        if (!string.IsNullOrEmpty(model.content))
        {
            var query = @"INSERT INTO message (author_id, text, pub_date, flagged)  
                            VALUES (@Author_id, @Text, @Pub_date, @Flagged)";

            var parameters = new Dictionary<string, object> {
            {"@Author_id", GetUserID(username)},
            {"@Text", model.content},
            {"@Pub_date", (long)DateTimeOffset.Now.ToUnixTimeSeconds()},
            {"@Flagged", 0}
        };
            await _databaseService.QueryDb<dynamic>(query, parameters);
            return NoContent();
        }
        return BadRequest();
    }


    [HttpGet("msgs/{username}")]
    public async Task<IActionResult> GetMessagesPerUser(string username, int no, string latest)

    {
        var userID = GetUserID(username);
        if (userID == -1)
        {
            return NotFound();
        }
        var sqlQuery = @"
            SELECT message.*, user.*
            FROM message
            INNER JOIN user ON message.author_id = user.user_id
            WHERE user.username = @Username AND message.flagged = 0
            ORDER BY message.pub_date DESC
            LIMIT @PerPage";

        var dict = new Dictionary<string, object> { { "@Username", username }, { "@PerPage", no } };
        var queryResult = await _databaseService.QueryDb<dynamic>(sqlQuery, dict);

        var filteredMsgs = queryResult.Select(msg =>
        {
            var dictB = (IDictionary<string, object>)msg;
            return new APIMessageModel
            {
                content = (string)dictB["text"],
                pub_date = (long)dictB["pub_date"],
                user = (string)dictB["username"]
            };
        }).ToList();
        return Ok(filteredMsgs);
    }


    [HttpGet("fllws/{username}")]
    public async Task<IActionResult> GetUserFollowers(string username, string latest)
    {
        updateLatest(latest);

        var notFromSimResponse = NotReqFromSimulator(Request);
        if (notFromSimResponse != null)
            return Forbid(notFromSimResponse.ErrorMessage);

        var userID = GetUserID(username);
        if (userID == -1)
            return NotFound();

        var noFollowers = Convert.ToInt32(Request.Query["no"].FirstOrDefault() ?? "100");

        var sqlQuery = @"
            SELECT user.username
            FROM user
            INNER JOIN follower ON follower.whom_id = user.user_id
            WHERE follower.who_id = @UserId
            LIMIT @Limit";

        var parameters = new Dictionary<string, object>
        {
            { "@UserId", userID },
            { "@Limit", noFollowers }
        };

        var queryResult = await _databaseService.QueryDb<dynamic>(sqlQuery, parameters);

        var followerNames = queryResult.Select(row => (string)row.username).ToList();

        var followersResponse = new
        {
            follows = followerNames
        };

        return Ok(followersResponse);
    }
public class FollowActionDto
{
    public string? Follow { get; set; }
    public string? Unfollow { get; set; }
}



    [HttpPost("fllws/{username}")]
    public async Task<IActionResult> FollowUser(string username, string latest, [FromBody] FollowActionDto followAction )
    {
        updateLatest(latest);

        // Check if request is from simulator
        var notFromSimResponse = NotReqFromSimulator(Request);
       if (notFromSimResponse != null)
            return Forbid(notFromSimResponse.ErrorMessage);

        var userID = GetUserID(username);
        if (userID == -1)
            return NotFound();

        // Extract the username to follow from the request body
        if (!string.IsNullOrEmpty(followAction.Follow))
        {
            // Access the 'follow' property from the model
            var followsUsername = followAction.Follow;

            var followsUserId = GetUserID(followsUsername);
            if (followsUserId == -1)
                return NotFound($"User '{followsUsername}' not found.");

            // Insert the follow relationship into the database
            var sqlQuery = "INSERT INTO follower (who_id, whom_id) VALUES (@WhoId, @WhomId)";
            var parameters = new Dictionary<string, object>
        {
            { "@WhoId", userID },
            { "@WhomId", followsUserId }
        };

            await _databaseService.QueryDb<int>(sqlQuery, parameters);

            return Ok($"Successfully followed user '{followsUserId}'.");
        }

        // TODO: Implement unfollowing
        if (!string.IsNullOrEmpty(followAction.Unfollow))
        {
            var unfollowsUsername = followAction.Unfollow;

            var unfollowsUserId = GetUserID(unfollowsUsername);
            if (unfollowsUserId == -1)
                return NotFound($"User '{unfollowsUsername}' not found.");

            var sqlQuery = "DELETE FROM follower WHERE who_id = @WhoId AND whom_id = @WhomId";
            var parameters = new Dictionary<string, object>
        {
            { "@WhoId", userID },
            { "@WhomId", unfollowsUserId }
        };

            await _databaseService.QueryDb<int>(sqlQuery, parameters);

            return Ok($"Successfully unfollowed user '{unfollowsUserId}'.");
        }
        return BadRequest("Invalid request.");
    }
}

