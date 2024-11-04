namespace DeclutterHub.Models
{
    public class CategoryDropdownViewModel
    {
        public IEnumerable<CategoryViewModel> Categories { get; set; }
        public string SelectedCategoryId { get; set; }
    }
}
