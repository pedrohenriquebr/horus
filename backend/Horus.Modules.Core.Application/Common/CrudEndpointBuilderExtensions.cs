namespace Horus.Modules.Core.Application.Common;

public static class CrudEndpointBuilderExtensions
{
    public static CrudEndpointBuilder ToggleCreate(this CrudEndpointBuilder builder, bool enable)
    {
        return enable ? builder : builder.DisableCreate();
    }

    public static CrudEndpointBuilder ToggleUpdate(this CrudEndpointBuilder builder, bool enable)
    {
        return enable ? builder : builder.DisableUpdate();
    }

    public static CrudEndpointBuilder ToggleDelete(this CrudEndpointBuilder builder, bool enable)
    {
        return enable ? builder : builder.DisableDelete();
    }

    public static CrudEndpointBuilder ToggleGetAll(this CrudEndpointBuilder builder, bool enable)
    {
        return enable ? builder : builder.DisableGetAll();
    }

    public static CrudEndpointBuilder ToggleRead(this CrudEndpointBuilder builder, bool enable)
    {
        return enable ? builder : builder.DisableRead();
    }

    public static CrudEndpointBuilder ToggleAntiforgery(this CrudEndpointBuilder builder, bool enable)
    {
        return enable ? builder.DisableAntiforgery() : builder;
    }
}