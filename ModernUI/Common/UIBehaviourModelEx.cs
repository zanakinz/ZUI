namespace ModernUI.Common;

/// <summary>
/// A class which can be used as an abstract UI object, which does not exist as a Component but which can receive Update calls.
/// </summary>
public abstract class UIBehaviourModelEx : UIModelEx
{
    public UIBehaviourModelEx()
    {
        ModernUI.AddBehaviourModel(this);
    }

    public virtual void Update()
    {
    }

    public override void Destroy()
    {
        ModernUI.DestroyBehaviourModel(this);
        base.Destroy();
    }
}