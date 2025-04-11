namespace Container_Testing.Models
{
    public class ResponseModels<T>
    {
        public int? StatusCode { get; set; } = 200;
        public string? Message { get; set; }
        public List<T>? Data { get; set; }
    }
}
