using ApiAccessLibrary.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiAccessLibrary.Interfaces
{
    public interface IiProGatewayServices
    {
        Task<string> PostSaleIProGateway(SaleRequestModel model);
        Task<string> PostSaleByTransactionIdIProGateway(SaleRequestModel model);
        Task<string> PostInstallmentIProGateway(SaleRequestModel model);
        Task<string> PostAuthIProGateway(SaleRequestModel model);
    } 
}
