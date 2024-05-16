using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TaskTracker.DBContext;
using TaskTracker.Hubs;
using TaskTracker.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TaskTracker.DataAccessLayer
{
    public class DataAccess
    {
        private readonly TaskContext dbContext;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<Notifications, INotification> _hubContext;
        string? connectionString;
        public DataAccess(TaskContext dbContext,IConfiguration configuration, IHubContext<Notifications, INotification> notification)
        {
            this.dbContext = dbContext;
            this._configuration = configuration;
            this._hubContext = notification;
            this.connectionString = this._configuration.GetConnectionString("mycon");
        }

        /// <summary>
        /// User registeration
        /// </summary>
        /// <param name="user">User instance </param>
        /// <returns>Boolean</returns>
        public async Task<bool> UserRegister(User user)
        {
            try
            {
                await dbContext.Users.AddAsync(user);
                return true;

            }
            finally {
                await dbContext.SaveChangesAsync();
            }

        }


        /// <summary>
        /// Login logic
        /// </summary>
        /// <param name="login">Login instance(Username and Password)</param>
        /// <returns>Boolean</returns>
        public async Task<string> Login(Login login)
        {
            try
            {
                var res =await dbContext.Users.FirstOrDefaultAsync(user => user.Username == login.Username && user.Password == login.Password);
                if (res != null)
                {
                    ConnectedUsers.UserId = res.UserId;
                    string token = GenerateToken(res);
                    return token;
                }
                else
                    return null;   
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Add task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<bool> AddTask(UserTask task)
        {
            try
            {
                task.Status = "Assigned";
                await dbContext.UserTasks.AddAsync(task);
                return true;
            }
            finally {
                await dbContext.SaveChangesAsync();
            }
        }

        public List<UserDetails> GetUsers()
        {
            try
            {
                var users = dbContext.Users
                 .Select(u => new UserDetails
                 {
                     UserId = u.UserId,
                     Name = u.Name,
                     PhoneNumber = u.PhoneNumber,
                     Address = u.Address,
                     Email = u.Email,
                     Username = u.Username
                 })
                 .ToList();
                return users;
            }
            finally { }
        }
        //Get user details
        public UserDetails GetUserById(int userId)
        {
            try
            {
                var user = dbContext.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserDetails
                    {
                        UserId = u.UserId,
                        Name = u.Name,
                        PhoneNumber = u.PhoneNumber,
                        Address = u.Address,
                        Email = u.Email,
                        Username = u.Username
                    })
                    .SingleOrDefault();

                return user;
            }
            finally { }
        }


        //Get user by name or username
        public List<UserDetails> GetUsers(string searchString)
        {
            try
            {
                var users = dbContext.Users
                    .Where(u => u.Name.Contains(searchString) || u.Username.Contains(searchString))
                    .Select(u => new UserDetails
                    {
                        UserId = u.UserId,
                        Name = u.Name,
                        PhoneNumber = u.PhoneNumber,
                        Address = u.Address,
                        Email = u.Email,
                        Username = u.Username
                    })
                    .ToList();
                return users;
            }
            finally { }
        }

        /// <summary>
        /// Get all tasks
        /// </summary>
        /// <returns></returns>
        public List<TaskDetails> GetTask()
        {
            try
            {
                List<TaskDetails> taskList = new List<TaskDetails>();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("SP_GetTaskDetails", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        DataTable dataTable = new DataTable();

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        foreach (DataRow row in dataTable.Rows)
                        {
                            TaskDetails task = new TaskDetails
                            {
                                TaskId = (int)row["TaskId"],
                                Title = row["Title"].ToString(),
                                Description = row["Description"].ToString(),
                                Status = row["Status"].ToString(),
                                AssigneeId = (int)row["AssigneeId"],
                                AssigneeUsername = row["AssigneeUsername"].ToString(),
                                AssigneeName = row["AssigneeName"].ToString(),
                                CreatorId = (int)row["CreatorId"],
                                CreatorName = row["CreatorName"].ToString(),
                                CreatedAt = (DateTime)row["CreatedAt"],
                                UpdatedAt = (DateTime)row["UpdatedAt"]
                            };

                            taskList.Add(task);
                        }
                    }
                }
                return taskList;
            }
            finally
            {
                
            }
        }


        /// <summary>
        /// Update task details 
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<bool> UpdateTask(TaskUpdate task)
        {
            try
            {
               using(SqlConnection con= new SqlConnection(connectionString))
                {
                    using(SqlCommand cmd = new SqlCommand("SP_UpdateUserTask",con)) {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TaskId",task.TaskId);
                        cmd.Parameters.AddWithValue("@Title", task.Title);
                        cmd.Parameters.AddWithValue("@Description", task.Description);
                        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AssigneeId", task.AssigneeId);
                        await con.OpenAsync();
                        int i =await cmd.ExecuteNonQueryAsync();
                        if (i > 0)
                        {
                            Notification notification = new Notification() { 
                                Content=$"{task.Title}:Details updated",
                                UserId=task.AssigneeId,
                                CreatedAt=DateTime.Now,
                                
                            };
                            dbContext.Notifications.Add(notification);
                            dbContext.SaveChanges();
                         }
                        
                        return i> 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> TaskStatusUpdate(TaskStatusUpdate task)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_TaskStatusUpdate", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TaskId", task.TaskId);
                        cmd.Parameters.AddWithValue("@Status", task.Status);
                        await con.OpenAsync();
                        int i = await cmd.ExecuteNonQueryAsync();
                        if (i > 0)
                        {
                            var selectedTask = GetTask().FirstOrDefault(t => t.TaskId == task.TaskId);
                            await SendMessage($"{selectedTask.Title}<br> Status changed to {task.Status}", selectedTask.CreatorId);

                            Notification notification = new Notification()
                            {
                                Content = $"{selectedTask.Title}:Status changed",
                                UserId = selectedTask.CreatorId,
                                CreatedAt = DateTime.Now,

                            };
                            dbContext.Notifications.Add(notification);
                            dbContext.SaveChanges();
                        }
                        return i > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        //Accept the task
        public async Task<bool> TaskAccept(int taskId)
        {
            try
            {
                
                using(SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand com = new SqlCommand("SP_TaskAccept", con))
                    {
                        com.CommandType = System.Data.CommandType.StoredProcedure;
                        com.Parameters.AddWithValue("@TaskId", taskId);
                        await con.OpenAsync();
                        int i = await com.ExecuteNonQueryAsync();
                        var task = GetTask().FirstOrDefault(task => task.TaskId == taskId);
                        await SendMessage($"{task.Title}<br> Status updated", task.CreatorId);
                        return i > 0;
                    }
                }
            }catch(Exception ex)
            {
                return false;
            }
        }

        //Set task status to complted
        public async Task<bool> TaskCompleted(int taskId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand com = new SqlCommand("SP_TaskComplete", con))
                    {
                        com.CommandType = System.Data.CommandType.StoredProcedure;
                        com.Parameters.AddWithValue("@TaskId", taskId);
                        await con.OpenAsync();
                        int i = await com.ExecuteNonQueryAsync();
                        
                        //Send notification
                        var task = GetTask().FirstOrDefault(task => task.TaskId == taskId);
                        await SendMessage($"{task.Title}<br> Completed",task.CreatorId);
                        return i > 0;
                    }
                }
            }

            catch (Exception ex)
            {
                return false;
            }
        }

        public List<Notification> GetNotifications(int id)
        {
            try
            {
                var notification =dbContext.Notifications.Where(n=>n.UserId == id).ToList();
                return notification;

            }finally { }
        }
        private string GenerateToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var payload = new[]
            {
                new Claim("username",user.Username),
                new Claim("userId",user.UserId.ToString()),
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], payload,
                expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> SendMessage(string message, int id)
        {
            var connectIdDictionary = ConnectedUsers.ConnectId.FirstOrDefault(d => d.ContainsKey(id));
            if (connectIdDictionary == null)
            {
                return false;
            }
            string connectionId = connectIdDictionary[id];
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendNotification( message);
                return true;
            }
            return false;
        }


        public async Task<bool> SendMessageNon(string message, string connectionId)
        {

            await _hubContext.Clients.Client(connectionId).SendNotification(message);

            return false;
        }

    }
}

