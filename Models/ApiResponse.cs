namespace PromptTrackerv1.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }

        public ApiResponse(bool success, string message, T? data = default)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        // helpers for success and fail
        public static ApiResponse<T> Ok(string message, T? data = default)
            => new ApiResponse<T>(true, message, data);

        public static ApiResponse<T> Fail(string message)
            => new ApiResponse<T>(false, message);
    }
}
