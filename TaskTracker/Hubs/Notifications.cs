using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskTracker.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TaskTracker.Hubs
{
    public class Notifications:Hub<INotification>
    {
        public async Task SendNotification(string message)
        {
            await Clients.All.SendNotification(message);
        }
        public override Task OnConnectedAsync()
        {
            // Extract userId from the query string
            if (int.TryParse(Context.GetHttpContext().Request.Query["userId"], out int userId))
            {
                AddConnectedUser(userId, Context.ConnectionId);
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotificationToUser(string userId, string message)
        {
            await Clients.Client(userId).SendNotification(message);
        }

        //Add connected user
        public void AddConnectedUser(int userId, string connectionId)
        {
            var userDictionary = ConnectedUsers.ConnectId.FirstOrDefault(dict => dict.ContainsKey(userId));

            if (userDictionary != null)
            {
                userDictionary[userId] = connectionId;
            }
            else
            {
                ConnectedUsers.ConnectId.Add(new Dictionary<int, string> { { userId, connectionId } });
            }
        }
        public void RemoveConnectedUser(string connectionId )
        {
            var userDictionary = ConnectedUsers.ConnectId.FirstOrDefault(dict => dict.ContainsValue(connectionId));

            if (userDictionary != null)
            {
                var keyToRemove = userDictionary.First(kvp => kvp.Value == connectionId).Key;
                userDictionary.Remove(keyToRemove);
            }

        }


    }


}
