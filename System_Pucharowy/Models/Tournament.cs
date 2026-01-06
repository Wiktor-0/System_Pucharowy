namespace System_Pucharowy.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = "PLANNED";

        public List<User> Participants { get; set; } = new();
        public Bracket? Bracket { get; set; }
    }
}
