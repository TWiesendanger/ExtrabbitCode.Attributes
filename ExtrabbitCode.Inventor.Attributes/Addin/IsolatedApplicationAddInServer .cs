using System;

namespace ExtrabbitCode.Inventor.Attributes.Addin;

public abstract class IsolatedApplicationAddInServer : ApplicationAddInServer
{
    private object? _isolatedInstance;

    public object? Automation { get; set; }

    public ApplicationAddInSite ApplicationAddInSite { get; private set; } = null!;

    public bool FirstTime { get; private set; }


    // ReSharper disable once ParameterHidesMember
    public void Activate(ApplicationAddInSite AddInSiteObject, bool FirstTime)
    {
        Type currentType = GetType();

        if (!AddinLoadContext.CheckIfCustomContext(currentType))
        {
            AddinLoadContext dependenciesProvider = AddinLoadContext.GetDependenciesProvider(currentType);
            _isolatedInstance = dependenciesProvider.CreateInstance(currentType);

            AddinLoadContext.Invoke(_isolatedInstance, nameof(Activate), AddInSiteObject, FirstTime);
            return;
        }

        ApplicationAddInSite = AddInSiteObject;
        this.FirstTime = FirstTime;

        OnActivate();
    }

    public void Deactivate()
    {
        Type currentType = GetType();

        if (!AddinLoadContext.CheckIfCustomContext(currentType))
        {
            AddinLoadContext.Invoke(_isolatedInstance!, nameof(Deactivate));
            return;
        }

        OnDeactivate();
    }

    [Obsolete("Deprecated in the Inventor API. Required for legacy compatibility.")]
    public void ExecuteCommand(int CommandID) { }

    /// <summary>
    ///	Overload this method to execute custom logic when the Inventor Addin is loaded and <see cref="ApplicationAddInServer.Activate" /> method is executed.
    /// </summary>
    public abstract void OnActivate();

    /// <summary>
    ///	Overload this method to execute custom logic when the Inventor Addin is unloaded and <see cref="ApplicationAddInServer.Deactivate" /> method is executed.
    /// </summary>
    public abstract void OnDeactivate();
}