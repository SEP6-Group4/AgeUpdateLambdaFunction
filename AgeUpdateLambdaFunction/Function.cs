using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Npgsql;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AgeUpdateLambdaFunction;

public class Functions
{
    List<User> users;
    List<User> ageGroupChangeUsers;
    string connectionString = "Host=sep6.c7szkct1z4j9.us-east-1.rds.amazonaws.com;Username=sep6;Password=dingdong69420!;Database=sep6";
    DateTime today;
    string todayString;

    public Functions()
    {
        users = new List<User>();
        ageGroupChangeUsers = new List<User>();
        today = DateTime.Today;
        string month;
        string day;

        if (today.Month < 10)
        {
            month = "0" + today.Month.ToString();
        }
        else
        {
            month = today.Month.ToString();
        }

        if (today.Day < 10)
        {
            day = "0" + today.Day.ToString();
        }
        else
        {
            day = today.Day.ToString();
        }

        todayString = "%-" + month + "-" + day;

        LambdaLogger.Log(todayString);
    }


    public async Task Get(APIGatewayProxyRequest request, ILambdaContext context)
    {
        await UpdateAllBirthdayUsersAge();
        await GetAllBirthdayUsers();
        await UpdateAgeGroup();

    }

    private async Task UpdateAllBirthdayUsersAge()
    {
        using var con = new NpgsqlConnection(connectionString);
        con.Open();

        string command = $"UPDATE public.\"User\" SET \"Age\"=\"Age\"+1 WHERE CAST(\"Birthday\" AS VARCHAR) LIKE @Today;";
        await using (NpgsqlCommand cmd = new NpgsqlCommand(command, con))
        {
            cmd.Parameters.AddWithValue("@Today", todayString);

            cmd.ExecuteNonQuery();
        }
        con.Close();
    }

    private static User ReadUser(NpgsqlDataReader reader)
    {
        try
        {
            User user = new User
            {
                UserID = reader["UserID"] as int?,
                FirstName = reader["First Name"] as string,
                LastName = reader["Last Name"] as string,
                Birthday = reader["Birthday"] as DateTime?,
                Email = reader["Email"] as string,
                Country = reader["Country"] as string,
                Password = reader["Password"] as string,
                AgeGroup = reader["AgeGroup"] as int?,
                Age = reader["Age"] as int?
            };
            return user;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    private async Task GetAllBirthdayUsers()
    {
        using var con = new NpgsqlConnection(connectionString);
        con.Open();

        string command = $"SELECT * FROM public.\"User\" where CAST(\"Birthday\" AS VARCHAR) LIKE @Today;";

        using (NpgsqlCommand cmd = new NpgsqlCommand(command, con))
        {
            cmd.Parameters.AddWithValue("@Today", todayString);

            await using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    users.Add(ReadUser(reader));
                }
        }
        con.Close();
        UpdateUserList();
    }

    private void UpdateUserList()
    {
        foreach (var user in users)
        {
            if (user.Age == 15 || user.Age == 22 || user.Age == 30 || user.Age == 41 || user.Age == 56)
            {
                ageGroupChangeUsers.Add(user);
            }
        }
    }

    private async Task UpdateAgeGroup()
    {
        using var con = new NpgsqlConnection(connectionString);
        con.Open();

        foreach (var user in ageGroupChangeUsers)
        {
            string command = $"UPDATE public.\"User\" SET \"AgeGroup\"=\"AgeGroup\"+1 WHERE \"UserID\" = @UserID;";
            await using (NpgsqlCommand cmd = new NpgsqlCommand(command, con))
            {
                cmd.Parameters.AddWithValue("@UserID", user.UserID);

                cmd.ExecuteNonQuery();
            }
        }
        con.Close();
        users.RemoveRange(0, users.Count);
        ageGroupChangeUsers.RemoveRange(0, ageGroupChangeUsers.Count);
    }
}

public class User
{
    [JsonPropertyName("UserID")]
    public int? UserID { get; set; }
    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }
    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }
    [JsonPropertyName("Birthday")]
    public DateTime? Birthday { get; set; }
    [JsonPropertyName("Email")]
    [EmailAddress]
    public string? Email { get; set; }
    [JsonPropertyName("Country")]
    public string? Country { get; set; }
    [JsonPropertyName("Password")]
    public string? Password { get; set; }
    [JsonPropertyName("AgeGroup")]
    public int? AgeGroup { get; set; }
    [JsonPropertyName("Age")]
    public int? Age { get; set; }
}