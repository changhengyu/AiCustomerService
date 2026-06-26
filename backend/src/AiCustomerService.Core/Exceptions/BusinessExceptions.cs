namespace AiCustomerService.Core.Exceptions;

public class BusinessException : Exception
{
    public int Code { get; }
    public BusinessException(string message, int code = 400) : base(message)
    {
        Code = code;
    }
}

public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message = "未授权") : base(message, 401) { }
}

public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message = "无权限") : base(message, 403) { }
}

public class QuotaExceededException : BusinessException
{
    public QuotaExceededException(string message = "已超出本月消息配额") : base(message, 429) { }
}

public class ValidationException : BusinessException
{
    public ValidationException(string message) : base(message, 422) { }
}
