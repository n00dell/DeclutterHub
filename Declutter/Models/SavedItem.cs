namespace DeclutterHub.Models
{
    public class SavedItem
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public int ItemId { get; set; }
        public DateTime SavedAt { get; set; }
        public User User { get; set; }
        public Item Item { get; set; }
    }
}
