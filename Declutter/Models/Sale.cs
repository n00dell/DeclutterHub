namespace DeclutterHub.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public int ItemId { get; set; }  // Reference to the item sold
        public DateTime SaleDate { get; set; }  // Date of the sale

        // Navigation property for the related item
        public virtual Item Item { get; set; }
        
    }
}
