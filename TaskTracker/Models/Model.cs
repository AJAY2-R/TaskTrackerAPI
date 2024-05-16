using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [MaxLength(100)]
        public string? Name { get; set; }
        [MaxLength(10)]
        public string? PhoneNumber { get; set; }
        [MaxLength(100)]
        public string? Address { get; set; }
        [MaxLength(100)]
        public string? Email { get; set; }
        [MaxLength(100)]
        public string? Username { get; set; }
        [MaxLength(200)]
        public string? Password { get; set; }


    }

    public class UserTask
    {
        [Key]
        public int TaskId { get; set; }

        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public int AssigneeId { get; set; }
        public User? Assignee { get; set; }

        public int CreatorId { get; set; }
        public User? Creator { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

   
    public class Login
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
    public class UserDetails
    {
        public int UserId { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TaskDetails
    {
        public int TaskId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public int AssigneeId { get; set; }
        public string? AssigneeUsername { get; set; }
        public string? AssigneeName { get; set; }
        public int CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TaskUpdate
    {
        public int TaskId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int AssigneeId { get; set; }
    }

    public class TaskStatusUpdate
    {
        public int TaskId { get; set;}
        public string? Status { get; set; }
    }
}
