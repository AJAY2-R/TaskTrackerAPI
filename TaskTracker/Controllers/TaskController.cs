using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using TaskTracker.DBContext;
using TaskTracker.Models;
using TaskTracker.DataAccessLayer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskTracker.Hubs;
using System.Threading.Tasks;

namespace TaskTracker.Controllers
{
    [Route("api")]
    [ApiController]
    [EnableCors("CORSPolicy")]
    public class TaskController : ControllerBase
    {
        private readonly DataAccess dataAccess;
        
        public TaskController(TaskContext dbContext,IConfiguration configuration,IHubContext<Notifications,INotification> _notification)
        {
            this.dataAccess = new DataAccess(dbContext,configuration,_notification);
        }

        /// <summary>
        /// User registration
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromBody] User user) {
            
            if(await dataAccess.UserRegister(user))
                return Ok();
            else
                 return BadRequest();
        }
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult> Login(Login login)
        {
            try
            {
                string token = await dataAccess.Login(login);
                if(token!=null)
                {
                    var obj= new { token = token };
                    return Ok(obj);
                }
                else
                    return Unauthorized();
            } catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("AddTask")]
        public async Task<ActionResult> AddTask(UserTask task)
        {
            var userIdClaim = User.FindFirst("userId");

            if (int.TryParse(userIdClaim.Value, out int userId))
            {
                task.CreatorId = userId;

                if (await dataAccess.AddTask(task))
                {
                    await dataAccess.SendMessage("New task assigned", task.AssigneeId);
                    return Ok();
                }
                else
                    return BadRequest();
            }
            return BadRequest();
        }


        [HttpGet("Users")]
        public ActionResult GetUsers() {
            try
            {
                var res= dataAccess.GetUsers();
                return Ok(res);
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("Users/{search}")]
        public ActionResult SearchUser(string search)
        {
            try
            {
                var res=dataAccess.GetUsers(search);
                return Ok(res);

            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpGet("Tasks")]
        public ActionResult GetTasks()
        {
            try
            {
                return Ok(dataAccess.GetTask());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("UserTasks")]
        public ActionResult GetTaskUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");

                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    int id = userId;

                    return Ok(dataAccess.GetTask().Where(t => t.AssigneeId == id ));
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Task/{id}")]
        public ActionResult GetTask(int id)
        {
            try
            {
                return Ok(dataAccess.GetTask().FirstOrDefault(task=>task.TaskId ==  id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetUser/{id}")]
        public ActionResult GetUser(int id)
        {
            try
            {
                return Ok(dataAccess.GetUserById(id));
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);  
            }
        }

        [HttpPost("UpdateTask")]
        public async Task< ActionResult> UpdateTask([FromBody]TaskUpdate task)
        {
            try
            {
               var res= await dataAccess.UpdateTask(task);
                await dataAccess.SendMessage("Task updated", task.AssigneeId);
                return Ok();
            }catch( Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("TaskAccept")]
        public async Task<ActionResult> TaskAccept([FromBody]int taskId)
        {
            try
            {
                if (await dataAccess.TaskAccept(taskId))
                {
                    return Ok();
                }
                else
                    return BadRequest();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("TaskStatusUpdate")]
        public async Task<ActionResult> TaskStatusUpdate([FromBody] TaskStatusUpdate task)
        {
            try
            {
                if (await dataAccess.TaskStatusUpdate(task))
                {
                   return Ok();
                }
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("TaskCompleted")]
        public async Task<ActionResult> TaskCompleted([FromBody] int taskId)
        {
            try
            {
                if (await dataAccess.TaskCompleted(taskId))
                    return Ok();
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("Notifications/{id}")]
        public ActionResult GetNotifications(int id) {
            try
            {
                return Ok(dataAccess.GetNotifications(id));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Connections")]
        public ActionResult GetConnection()
        {
            return Ok(ConnectedUsers.ConnectId);
        }
        [HttpPost("SendMessage")]
        public async Task<ActionResult> SendMessage(string message,string id) {

            if (await dataAccess.SendMessageNon(message, id))
                return Ok();
            else return BadRequest();
        }
    }

}
