namespace DeclutterHub.Models
{
    public class Image
    {
        public int Id { get; set; }
        public string Url { get; set; }  // The location of the image file (path or URL)

        // Foreign key to reference the item the image is associated with
        public int ItemId { get; set; }
        public virtual Item Item { get; set; }
    }
}
