using Microsoft.OpenApi.Models;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchoolMedicalManagementSystem.API.OperationFilters;

public class GenericResponseTypeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var controllerType = context.MethodInfo.DeclaringType;

        if (!controllerType.IsGenericType)
            return;

        var responseType = controllerType.GetGenericArguments()[1]; // TResponse

        // Add error schemas for common responses
        var errorSchema = context.SchemaGenerator.GenerateSchema(
            typeof(BaseResponse<>).MakeGenericType(typeof(object)),
            context.SchemaRepository
        );

        switch (context.ApiDescription.HttpMethod.ToUpper())
        {
            case "GET":
                if (context.MethodInfo.Name == "GetById")
                {
                    SetResponseSchema(
                        operation,
                        "200",
                        typeof(BaseResponse<>).MakeGenericType(responseType),
                        context
                    );
                    operation.Responses["404"] = new OpenApiResponse
                    {
                        Description = "Not Found",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = errorSchema,
                            },
                        },
                    };
                }
                else if (context.MethodInfo.Name == "GetPaged")
                {
                    SetResponseSchema(
                        operation,
                        "200",
                        typeof(BaseListResponse<>).MakeGenericType(responseType),
                        context
                    );
                }
                else // GetAll
                {
                    SetResponseSchema(
                        operation,
                        "200",
                        typeof(BaseResponse<>).MakeGenericType(
                            typeof(List<>).MakeGenericType(responseType)
                        ),
                        context
                    );
                }

                break;

            case "POST":
                if (context.MethodInfo.Name == "CreateRange")
                {
                    SetResponseSchema(
                        operation,
                        "201",
                        typeof(BaseResponse<>).MakeGenericType(responseType),
                        context
                    );
                }
                else // Create
                {
                    SetResponseSchema(
                        operation,
                        "201",
                        typeof(BaseResponse<>).MakeGenericType(responseType),
                        context
                    );
                }

                break;

            case "PUT":
                SetResponseSchema(
                    operation,
                    "200",
                    typeof(BaseResponse<>).MakeGenericType(responseType),
                    context
                );
                operation.Responses["404"] = new OpenApiResponse
                {
                    Description = "Not Found",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType { Schema = errorSchema },
                    },
                };
                break;

            case "DELETE":
                SetResponseSchema(operation, "200", typeof(BaseResponse<bool>), context);
                operation.Responses["404"] = new OpenApiResponse
                {
                    Description = "Not Found",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType { Schema = errorSchema },
                    },
                };
                break;
        }

        // Add common error responses
        operation.Responses["400"] = new OpenApiResponse
        {
            Description = "Bad Request",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Schema = errorSchema },
            },
        };
        operation.Responses["500"] = new OpenApiResponse
        {
            Description = "Internal Server Error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Schema = errorSchema },
            },
        };
    }

    private void SetResponseSchema(
        OpenApiOperation operation,
        string statusCode,
        Type type,
        OperationFilterContext context
    )
    {
        var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses[statusCode] = new OpenApiResponse();
        }

        if (operation.Responses[statusCode].Content == null)
        {
            operation.Responses[statusCode].Content =
                new Dictionary<string, OpenApiMediaType>();
        }

        operation.Responses[statusCode].Content["application/json"] = new OpenApiMediaType
        {
            Schema = schema,
        };
    }
}