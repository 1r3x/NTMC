using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiAccessLibrary.ApiModels;
using ApiAccessLibrary.Interfaces;
using DataAccessLibrary.Interfaces;
using EntityModelLibrary.Models;
using EntityModelLibrary.ViewModels;
using NTMC.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NTMC.CryptoGraphy;

namespace NTMC.Pages.PreSchedulePosting
{
    public partial class PreSchedulePosting
    {
        [Inject] private IGetPreSchedulePaymentInfo GetPreSchedulePaymentInfo { get; set; }
        [Inject] private IGetDetailsOfPreSchedulePayment GetDetailsOfPreSchedulePayment { get; set; }
        [Inject] private IAddCcPayment AddCcPayment { get; set; }
        [Inject] private IiProGatewayServices SaleApi { get; set; }
        [Inject] private IAddNotes AddNotes { get; set; }
        [Inject] private IAddPaymentScheduleHistory AddPaymentScheduleHistory { get; set; }
        [Inject] private IAddPaymentSchedule AddPaymentSchedule { get; set; }
        [Inject] private ICryptoGraphy Crypto { get; set; }


        private IList<LcgPaymentSchedule> _paymentSchedule;
        private LcgTablesViewModel _preScheduleLcgTablesViewModel;


        private IProGatewayRersponseModel _responseModel;
        private readonly DateTime _scheduleDateTime = DateTime.Now;
        private string _errorModel;
        private decimal _tempAmount;
        private int _loadingBar;
        private bool _busyClick;
        // username 
        private string _username;
        private async Task ProcessUserNameData()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity != null)
            {

                if (user.Identity is { IsAuthenticated: true })
                {
                    if (user.Identity.Name != null)
                    {
                        var username = user.Identity.Name.Split("\\");
                        _username = username[1].Length > 5
                            ? username[1][..5] == "admin" ? "31950" : username[1]
                            : username[1];
                    }
                }
                else
                {
                    _username = "NTMC";
                }
            }
        }
        // end

        private double _totalBar;
        private double _processedBar;
        private double _unProcessedBar;
        private double _progressBarPercentage;
        private double _unProcessedBarPercentage;


        protected override async Task OnInitializedAsync()
        {
            _paymentSchedule = await GetPreSchedulePaymentInfo.GetAllPreSchedulePaymentInfo(_centralizeVariablesModel.Value.DbEnvironment);
            _loadingBar = 0;
            await RefreshProgessBar();

            await ProcessUserNameData();

        }

        private Task RefreshProgessBar()
        {
            _unProcessedBar = 0;
            _processedBar = 0;

            for (int i = 0; i < _paymentSchedule.Count; i++)
            {
                if (_paymentSchedule[i].IsActive == true)
                {
                    _unProcessedBar += 1;
                }
                else
                {
                    _processedBar += 1;

                }

            }
            _progressBarPercentage = _processedBar / _paymentSchedule.Count * 100;
            _unProcessedBarPercentage = _unProcessedBar / _paymentSchedule.Count * 100;
            _totalBar = _processedBar + _unProcessedBar;
            return Task.CompletedTask;
        }

        async Task PostAllCC()
        {
            for (int i = 0; i < _paymentSchedule.Count; i++)
            {
                if (_paymentSchedule[i].IsActive == true)
                {
                    await OpenOrder(_paymentSchedule[i].Id);
                }

            }
        }

        async Task OpenOrder(int orderId)
        {
            _responseModel = null;
            _errorModel = null;
            _busyClick = true;
            _loadingBar = 1;
            _preScheduleLcgTablesViewModel =
                await GetDetailsOfPreSchedulePayment.GetDetailsOfPreSchedulePaymentInfo(orderId, _centralizeVariablesModel.Value.DbEnvironment);
            _tempAmount = _preScheduleLcgTablesViewModel.Amount;
            await ProcessSaleTrans();
            _loadingBar = 0;
            _paymentSchedule = await GetPreSchedulePaymentInfo.GetAllPreSchedulePaymentInfo(_centralizeVariablesModel.Value.DbEnvironment);
            await RefreshProgessBar();
            StateHasChanged();
            _busyClick = false;
            

        }
        private async Task ProcessSaleTrans()
        {
            _responseModel = null;
            _errorModel = null;
            var (Key, IVBase64) = Crypto.InitSymmetricEncryptionKeyIV();
            var decryptedCC = Crypto.Decrypt(_preScheduleLcgTablesViewModel.PaymentMethodId, IVBase64, Key);
            var saleRequestModel = new SaleRequestModel()
            {
                Outlet = new ApiAccessLibrary.ApiModels.Outlet()
                {
                    MerchantID = _centralizeVariablesModel.Value.Outlet.MerchantID,
                    StoreID = _centralizeVariablesModel.Value.Outlet.StoreID,
                    TerminalID = _centralizeVariablesModel.Value.Outlet.TerminalID
                },
                Amount = _preScheduleLcgTablesViewModel.Amount,
                PaymentMethod = "OnFile",
                Card = new ApiAccessLibrary.ApiModels.Card()
                {
                    CVN = _preScheduleLcgTablesViewModel.BinNumber,
                   
                    CardNumber = decryptedCC,//
                    EntryMode = "key",
                    Expiration = _preScheduleLcgTablesViewModel.ExpirationMonth+""+_preScheduleLcgTablesViewModel.ExpirationYear,
                   
                },
                PaymentMethodID = _preScheduleLcgTablesViewModel.PaymentMethodId,
                Patient = new ApiAccessLibrary.ApiModels.Patient()
                {
                    AccountNumber = _preScheduleLcgTablesViewModel.PatientAccount
                }
            };
            try
            {
                var resultVerify = await SaleApi.PostSaleIProGateway(saleRequestModel);
                if (resultVerify.Contains("FieldErrors"))
                {
                    _errorModel = resultVerify;
                }
                else
                {
                    _responseModel = new IProGatewayRersponseModel(resultVerify);

                }

                string noteText = null;
                if (@_responseModel != null && _responseModel.response == "100")
                {
                    noteText = "INSTAMED CC APPROVED FOR $" + _tempAmount + " " + @_responseModel.responsetext.ToUpper() +
                                  " AUTH #:" + @_responseModel.authcode;
                    await SaveCardInfoAndScheduleData();

                }
                else
                {
                    if (@_responseModel != null)
                        noteText = "INSTAMED CC DECLINED FOR $" + _tempAmount + " " +
                                   @_responseModel.responsetext.ToUpper() +
                                   " AUTH #:" + @_responseModel.authcode;
                }
                //actual employee =31950
                await AddNotes.Notes(_preScheduleLcgTablesViewModel.AssociateDebtorAcct, 31950, "RA", noteText, "N", null, _centralizeVariablesModel.Value.DbEnvironment);//PO for prod_old & T is for test_db


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


        }

        private async Task SaveCardInfoAndScheduleData()
        {
            try
            {

                var paymentScheduleExample = new LcgPaymentScheduleHistory()
                {
                    ResponseCode = _responseModel.response_code,
                    AuthorizationNumber = _responseModel.authcode,
                    AuthorizationText = _username,
                    ResponseMessage = _responseModel.responsetext,
                    PaymentScheduleId = _preScheduleLcgTablesViewModel.IdForPaymentSchedule,
                    TransactionId = _responseModel.transactionid
                };
                await AddPaymentScheduleHistory.SavePaymentScheduleHistory(paymentScheduleExample, _centralizeVariablesModel.Value.DbEnvironment);

                await AddPaymentSchedule.InactivePaymentSchedule(_preScheduleLcgTablesViewModel.IdForPaymentSchedule,
                    _centralizeVariablesModel.Value.DbEnvironment);

                var ccPaymentObj = new CcPayment()
                {
                    DebtorAcct = _preScheduleLcgTablesViewModel.PatientAccount,
                    Company = "TOTAL CREDIT RECOVERY",
                    UserId = _username,
                    UserName = _username + " -NTMC",
                    //UserId = "LCG",
                    //UserName = "PD -LCG",
                    ChargeTotal = _preScheduleLcgTablesViewModel.Amount,
                    Subtotal = _preScheduleLcgTablesViewModel.Amount,
                    PaymentDate = _scheduleDateTime,
                    ApprovalStatus = "APPROVED",
                    //todo card holder it must be saved from multiple page
                    BillingName = _preScheduleLcgTablesViewModel.CardHolderName,
                    ApprovalCode = _responseModel.response_code,
                    OrderNumber = _responseModel.transactionid,
                    RefNumber = "INSTAMEDLH",
                    Sif = "N"
                };
                await AddCcPayment.CreateCcPayment(ccPaymentObj, _centralizeVariablesModel.Value.DbEnvironment);
            }
            catch (Exception)
            {

                throw;
            }


        }


    }


}
