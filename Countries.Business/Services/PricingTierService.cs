using Countries.Domain.Enum;
using Countries.Domain.Repositories;

namespace Countries.Business.Services;

public class PricingTierService : IPricingTierService
{
    public PricingTier GetPricingTier(string ipAddress)
    {
        return PricingTier.Free;
    }
}