namespace Horus.Modules.Core.Application.Common;

public static class CrudGeneratorPatterns
{
    public static string GetByIdQueryPattern = "Get{0}ByIdQuery";
    public static string GetByIdQueryResponsePattern = "Get{0}ByIdQueryResponse";
    public static string GetAllQueryPattern = "GetAll{0}Query";
    public static string GetAllQueryResponsePattern = "GetAll{0}QueryResponse";
    public static string CreateCommandPattern = "Create{0}Command";
    public static string UpdateCommandPattern = "Update{0}Command";
    public static string DeleteCommandPattern = "Delete{0}Command";
}