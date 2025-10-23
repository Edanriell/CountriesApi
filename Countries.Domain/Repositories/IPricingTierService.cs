using Countries.Domain.Enum;

namespace Countries.Domain.Repositories;

public interface IPricingTierService
{
    public PricingTier GetPricingTier(string ipAddress);
}