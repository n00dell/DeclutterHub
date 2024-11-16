namespace DeclutterHub.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string UserName { get; set; }
        public DateTime JoinDate { get; set; }
        public string? AvatarUrl { get; set; }
        public List<Item> ActiveListings { get; set; }
        public int ItemsSoldCount { get; set; }
        public List<Item> SavedItems { get; set; }
        public List<Category> SuggestedCategories { get; set; }
        public List<Category> MostClickedCategories { get; set; }
    }
}
