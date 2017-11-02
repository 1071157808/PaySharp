using ICanPay.Core;
using ICanPay.Core.Utils;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ICanPay.Wechatpay
{
    /// <summary>
    /// ΢��֧������
    /// </summary>
    public sealed class WechatpayGataway : GatewayBase,
        IScanPayment, IAppPayment, IUrlPayment, IPublicPayment, IAppletPayment, IBarcodePayment,
        IQuery, ICancel, IRefund, IRefundQuery
    {

        #region ˽���ֶ�

        private readonly Merchant merchant;
        private const string USERPAYING = "USERPAYING";
        private const string UNIFIEDORDERGATEWAYURL = "https://api.mch.weixin.qq.com/pay/unifiedorder";
        private const string QUERYGATEWAYURL = "https://api.mch.weixin.qq.com/pay/orderquery";
        private const string CANCELGATEWAYURL = "https://api.mch.weixin.qq.com/secapi/pay/reverse";
        private const string CLOSEORDERGATEWAYURL = "https://api.mch.weixin.qq.com/pay/closeorder";
        private const string REFUNDGATEWAYURL = "https://api.mch.weixin.qq.com/secapi/pay/refund";
        private const string REFUNDQUERYGATEWAYURL = "https://api.mch.weixin.qq.com/pay/refundquery";
        private const string DOWNLOADBILLGATEWAYURL = "https://api.mch.weixin.qq.com/pay/downloadbill";
        private const string REPORTGATEWAYURL = "https://api.mch.weixin.qq.com/payitil/report";
        private const string BATCHQUERYCOMMENTGATEWAYURL = "https://api.mch.weixin.qq.com/billcommentsp/batchquerycomment";
        private const string BARCODEGATEWAYURL = "https://api.mch.weixin.qq.com/pay/micropay";

        #endregion

        #region ���캯��

        /// <summary>
        /// ��ʼ��΢��֧������
        /// </summary>
        /// <param name="merchant">�̻�����</param>
        public WechatpayGataway(Merchant merchant)
            : base(merchant)
        {
            this.merchant = merchant;
        }

        #endregion

        #region ����

        public override GatewayType GatewayType => GatewayType.Wechatpay;

        public override string GatewayUrl { get; set; } = UNIFIEDORDERGATEWAYURL;

        public new Merchant Merchant => merchant;

        public new Order Order => (Order)base.Order;

        public new Notify Notify => (Notify)base.Notify;

        protected override bool IsSuccessPay => Notify.TradeState.ToLower() == SUCCESS;

        protected override bool IsWaitPay => Notify.TradeState.ToLower() == USERPAYING;

        #endregion

        #region ����

        #region ɨ��֧��

        public string BuildScanPayment()
        {
            InitScanPayment();
            UnifiedOrder();
            return Notify.CodeUrl;
        }

        public void InitScanPayment()
        {
            Order.TradeType = Constant.NATIVE;
            Order.SpbillCreateIp = HttpUtil.LocalIpAddress.ToString();
        }

        #endregion

        #region App֧��

        public string BuildAppPayment()
        {
            InitAppPayment();
            UnifiedOrder();
            InitAppParameter();
            return GatewayData.ToJson();
        }

        public void InitAppPayment()
        {
            Order.TradeType = Constant.APP;
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// ��ʼ��APP�˵���֧���Ĳ���
        /// </summary>
        private void InitAppParameter()
        {
            GatewayData.Clear();
            Merchant.NonceStr = Util.GenerateNonceStr();
            GatewayData.Add(Constant.APPID, Merchant.AppId);
            GatewayData.Add(Constant.PARTNERID, Merchant.MchId);
            GatewayData.Add(Constant.PREPAYID, Notify.PrepayId);
            GatewayData.Add(Constant.PACKAGE, "Sign=WXPay");
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.TIMESTAMP, DateTime.Now.ToTimeStamp());
            GatewayData.Add(Constant.SIGN, BuildSign());
        }

        #endregion

        #region Url֧��

        public string BuildUrlPayment()
        {
            InitUrlPayment();
            UnifiedOrder();
            return Notify.MWebUrl;
        }

        public void InitUrlPayment()
        {
            Order.TradeType = Constant.MWEB;
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        #endregion

        #region ���ں�֧��

        public string BuildPublicPayment()
        {
            InitPublicPayment();
            UnifiedOrder();
            InitPublicParameter();
            return GatewayData.ToJson();
        }

        public void InitPublicPayment()
        {
            Order.TradeType = Constant.JSAPI;
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// ��ʼ�����ںŵ���֧���Ĳ���
        /// </summary>
        private void InitPublicParameter()
        {
            GatewayData.Clear();
            Merchant.NonceStr = Util.GenerateNonceStr();
            GatewayData.Add(Constant.APPID, Merchant.AppId);
            GatewayData.Add(Constant.TIMESTAMP, DateTime.Now.ToTimeStamp());
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.PACKAGE, $"{Constant.PREPAY_ID}={Notify.PrepayId}");
            GatewayData.Add(Constant.SIGN_TYPE, "MD5");
            GatewayData.Add(Constant.PAYSIGN, BuildSign());
        }

        #endregion

        #region С����֧��

        public string BuildAppletPayment()
        {
            InitAppletPayment();
            UnifiedOrder();
            InitPublicParameter();
            return GatewayData.ToJson();
        }

        public void InitAppletPayment()
        {
            Order.TradeType = Constant.JSAPI;
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        #endregion

        #region ����֧��

        public void BuildBarcodePayment()
        {
            InitBarcodePayment();

            Commit();

            if (!string.IsNullOrEmpty(Notify.TransactionId))
            {
                PollQueryTradeState(new Auxiliary
                {
                    TradeNo = Notify.TransactionId,
                    OutTradeNo = Notify.OutTradeNo
                });
            }
        }

        public void InitBarcodePayment()
        {
            GatewayUrl = BARCODEGATEWAYURL;
            Order.SpbillCreateIp = HttpUtil.LocalIpAddress.ToString();
        }

        /// <summary>
        /// ÿ��5����ѯ�ж��û��Ƿ�֧��,�ܹ���ѯ5��
        /// </summary>
        private void PollQueryTradeState(IAuxiliary auxiliary)
        {
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(5000);
                BuildQuery(auxiliary);
                if (IsSuccessPay)
                {
                    OnPaymentSucceed(new PaymentSucceedEventArgs(this));
                    return;
                }
            }

            BuildCancel(auxiliary);
            if (Notify.Recall == "Y")
            {
                BuildCancel(auxiliary);
            }
            OnPaymentFailed(new PaymentFailedEventArgs(this));
        }

        /// <summary>
        /// �첽ÿ��5����ѯ�ж��û��Ƿ�֧��,�ܹ���ѯ5��
        /// </summary>
        private async Task PollAsync(IAuxiliary auxiliary)
        {
            await Task.Run(() => PollQueryTradeState(auxiliary));
        }

        #endregion

        #region ��ѯ����

        public void InitQuery(IAuxiliary auxiliary)
        {
            GatewayUrl = QUERYGATEWAYURL;
            InitAuxiliaryParameter(auxiliary);
        }

        public INotify BuildQuery(IAuxiliary auxiliary)
        {
            InitQuery(auxiliary);

            Commit();

            return Notify;
        }

        #endregion

        #region ��������

        public void InitCancel(IAuxiliary auxiliary)
        {
            GatewayUrl = CANCELGATEWAYURL;
            InitAuxiliaryParameter(auxiliary);
        }

        public INotify BuildCancel(IAuxiliary auxiliary)
        {
            InitCancel(auxiliary);

            Commit(true);

            return Notify;
        }

        #endregion

        #region �����˿�

        public INotify BuildRefund(IAuxiliary auxiliary)
        {
            InitRefund(auxiliary);

            Commit(true);

            return Notify;
        }

        public void InitRefund(IAuxiliary auxiliary)
        {
            GatewayUrl = REFUNDGATEWAYURL;
            InitAuxiliaryParameter(auxiliary);
        }

        #endregion

        #region ��ѯ�˿�

        public INotify BuildRefundQuery(IAuxiliary auxiliary)
        {
            InitRefundQuery(auxiliary);

            Commit();

            return Notify;
        }

        public void InitRefundQuery(IAuxiliary auxiliary)
        {
            GatewayUrl = REFUNDQUERYGATEWAYURL;
            InitAuxiliaryParameter(auxiliary);
        }

        #endregion

        protected override async Task<bool> CheckNotifyDataAsync()
        {
            base.Notify = await GatewayData.ToObjectAsync<Notify>();

            if (IsSuccessResult())
            {
                return true;
            }

            return false;
        }

        private void InitOrderParameter()
        {
            Order.Amount *= 100;
            Merchant.NonceStr = Util.GenerateNonceStr();
            Merchant.DeviceInfo = Constant.WEB;
            GatewayData.Add(Merchant);
            GatewayData.Add(Order);
            Merchant.Sign = BuildSign();
            GatewayData.Add(Constant.SIGN, Merchant.Sign);
        }

        private void InitAuxiliaryParameter(IAuxiliary auxiliary)
        {
            auxiliary.Validate(GatewayAuxiliaryType.NoAction);
            Merchant.NonceStr = Util.GenerateNonceStr();
            GatewayData.Add(Merchant);
            GatewayData.Add(auxiliary);
            Merchant.Sign = BuildSign();
            GatewayData.Add(Constant.SIGN, Merchant.Sign);
        }

        public void InitFormPayment()
        {
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// ͳһ�µ�
        /// </summary>
        /// <returns></returns>
        private void UnifiedOrder()
        {
            GatewayUrl = UNIFIEDORDERGATEWAYURL;
            InitOrderParameter();

            ValidateParameter(Merchant);
            ValidateParameter(Order);

            Commit();
        }

        /// <summary>
        /// ��ȡ���ؽ��
        /// </summary>
        /// <param name="result"></param>
        private void ReadReturnResult(string result)
        {
            GatewayData.FromXml(result);
            base.Notify = GatewayData.ToObject<Notify>();
            IsSuccessReturn();
        }

        /// <summary>
        /// ���ǩ��
        /// </summary>
        /// <returns></returns>
        private string BuildSign()
        {
            string data = GatewayData.ToUrl(Constant.SIGN) + "&key=" + Merchant.Key;
            return EncryptUtil.MD5(data);
        }

        /// <summary>
        /// �Ƿ����ѳɹ�֧����֧��֪ͨ
        /// </summary>
        /// <returns></returns>
        private bool IsSuccessResult()
        {
            if (Notify.ReturnCode.ToLower() == SUCCESS && Notify.ResultCode.ToLower() == SUCCESS && Notify.Sign == BuildSign())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// �ύ����
        /// </summary>
        /// <param name="isCert">�Ƿ����֤��</param>
        private void Commit(bool isCert = false)
        {
            var cert = isCert ? new X509Certificate2(Merchant.SslCertPath, Merchant.SslCertPassword) : null;

            string result = HttpUtil
                .PostAsync(GatewayUrl, GatewayData.ToXml(), cert)
                .GetAwaiter()
                .GetResult();
            ReadReturnResult(result);
        }

        /// <summary>
        /// �Ƿ����ѳɹ��ķ���
        /// </summary>
        /// <returns></returns>
        private bool IsSuccessReturn()
        {
            if (Notify.ReturnCode == FAIL)
            {
                throw new Exception(Notify.ReturnMsg);
            }

            return true;
        }

        public override void WriteSuccessFlag()
        {
            GatewayData.Add(Constant.RETURN_CODE, SUCCESS);
            HttpUtil.Write(GatewayData.ToXml());
        }

        #endregion
    }
}
