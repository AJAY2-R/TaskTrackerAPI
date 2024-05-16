namespace TaskTracker.Hubs
{
    public interface INotification
    {
        Task SendNotification(string message);
    }
}
