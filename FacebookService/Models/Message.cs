

namespace FacebookService.Models
{
    public class Message
    {
        public string Id { get; set; }
        public string ParentId { get; set; } // ID du commentaire parent, si c'est une réponse
        public string Name { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
}
