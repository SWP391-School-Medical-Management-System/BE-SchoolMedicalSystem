namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }

    public BaseResponse()
    {
        Success = true;
        Errors = new List<string>();
    }

    public static BaseResponse<T> SuccessResult(T data, string message = "")
    {
        return new BaseResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
        };
    }

    public static BaseResponse<T> ErrorResult(string message, List<string> errors = null)
    {
        return new BaseResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>(),
        };
    }
}

public class BaseListResponse<T> : BaseResponse<List<T>>
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    public static BaseListResponse<T> SuccessResult(
        List<T> data,
        int totalCount = 0,
        int pageSize = 0,
        int currentPage = 0,
        string message = ""
    )
    {
        return new BaseListResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0,
        };
    }

    public static new BaseListResponse<T> ErrorResult(string message, List<string> errors = null)
    {
        return new BaseListResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>(),
        };
    }
}