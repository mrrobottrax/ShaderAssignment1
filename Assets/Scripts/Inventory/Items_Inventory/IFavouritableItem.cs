public interface IFavouritableItem
{
    public bool IsItemFavourited { get; }
    public int FavouriteSlotPointerID { get; }

    /// <summary>
    /// Abstract method to be implemented by child classes for using a favorited item.
    /// </summary>
    /// <remarks>
    /// This method should contain the logic for using a favorited item, specific to the child class implementation.
    /// </remarks>
    public abstract void UseFavouritedItem(PlayerHealth playerHealth);

    /// <summary>
    /// Abstract method to be implemented by child classes for favouriting an item.
    /// </summary>
    /// <remarks>
    /// This method should set the item as favourited and cache its pointer ID.
    /// </remarks>
    public abstract void FavouriteItem(int favouriteSlotID);

    /// <summary>
    /// Abstract method to be implemented by child classes for Unfavouriting an item.
    /// </summary>
    /// <remarks>
    /// This method should Unfavourite the item and pass out the ID of the slot pointer that needs to be unpaired.
    /// </remarks>
    public abstract void UnfavouriteItem(out int favouriteSlotID);
}
