namespace Janus.Domain.Models;

public class Entity
{
    #region Attributes
    public DateTime UpdatedAt  { get; protected set; }
    #endregion

    #region Methods
    public void Update<T>(T newValue, T currentValue, Action update)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            return;
        
        update();
        UpdatedAt = DateTime.UtcNow;
    }
    #endregion
}