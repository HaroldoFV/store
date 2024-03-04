using CartMicroservice.Models;

namespace CartMicroservice.Repository;

public interface ICartRepository
{
    IList<CartItem> GetCartItems(string userId);
    void InsertCartItem(string userId, CartItem cartItem);
    void UpdateCartItem(string userId, CartItem cartItem);
    void DeleteCartItem(string userId, string cartItem);
    void UpdateCatalogItem(string catalogItemId, string name, decimal price);
    void DeleteCatalogItem(string catalogItemId);
}