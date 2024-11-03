namespace DeclutterHub.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int ClickCount { get; set; }

        public string ImageUrl {  get; set; }
        public ICollection<Item> Items { get; set; }

        public bool IsApproved {  get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string CreatedBy {  get; set; }
        public bool IsActive { get; set; }
    }
}
