using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Materpiece.Models;

public class Queue
{
    [Key]
    public int QueueId { get; set; }

    [Required]
    public int StationId { get; set; }

    [ForeignKey("StationId")]
    public Station? Station { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty; 

    public DateTime RequestedTime { get; set; } = DateTime.UtcNow;

    public int Priority { get; set; } = 1; 
}