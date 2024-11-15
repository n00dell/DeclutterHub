namespace DeclutterHub.Models.ViewModels
{
    public class CategoryDropdownViewModel
    {
        public IEnumerable<CategoryViewModel> Categories { get; set; }
        public string SelectedCategoryId { get; set; }
    }
}
