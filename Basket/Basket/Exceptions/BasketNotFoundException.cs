using BuildingBlocks;

namespace Basket.Exceptions
{
    public class BasketNotFoundException : NotFoundException
    {
        public BasketNotFoundException(string userName)
            : base($"Basket with user name {userName} was not found.")
        {
        }
    }
}
