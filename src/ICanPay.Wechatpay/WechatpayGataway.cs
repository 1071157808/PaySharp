using ICanPay.Core;
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
        IQuery, ICancel
    {

        #region ˽���ֶ�

        private Merchant merchant;
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

            PollQueryTradeState();
        }

        public void InitBarcodePayment()
        {
            GatewayUrl = BARCODEGATEWAYURL;
            Order.SpbillCreateIp = HttpUtil.LocalIpAddress.ToString();
        }

        /// <summary>
        /// ÿ��5����ѯ�ж��û��Ƿ�֧��,�ܹ���ѯ5��
        /// </summary>
        private void PollQueryTradeState()
        {
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(5000);
                BuildQuery();
                if (IsSuccessPay)
                {
                    OnPaymentSucceed(new PaymentSucceedEventArgs(this));
                    return;
                }
            }

            BuildCancel();
            if (Notify.Recall == "Y")
            {
                BuildCancel();
            }
            OnPaymentFailed(new PaymentFailedEventArgs(this));
        }

        /// <summary>
        /// �첽ÿ��5����ѯ�ж��û��Ƿ�֧��,�ܹ���ѯ5��
        /// </summary>
        private async Task PollAsync()
        {
            await Task.Run(() => PollQueryTradeState());
        }

        #endregion

        #region ��ѯ����

        public void InitQuery()
        {
            GatewayUrl = QUERYGATEWAYURL;
            Merchant.NonceStr = Util.GenerateNonceStr();
            GatewayData.Add(Constant.APPID, Merchant.AppId);
            GatewayData.Add(Constant.MCH_ID, Merchant.MchId);
            GatewayData.Add(Constant.OUT_TRADE_NO, Order.OutTradeNo);
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.SIGN_TYPE, Merchant.SignType);
            GatewayData.Add(Constant.SIGN, BuildSign());
        }

        public INotify BuildQuery()
        {
            InitQuery();

            Commit();

            return Notify;
        }

        #endregion

        #region ��������

        public void InitCancel()
        {
            InitQuery();
            GatewayUrl = CANCELGATEWAYURL;
        }

        public INotify BuildCancel()
        {
            InitCancel();

            Commit(true);

            return Notify;
        }

        #endregion

        protected override async Task<bool> CheckNotifyDataAsync()
        {
            await ReadNotifyAsync<Notify>();

            if (IsSuccessResult())
            {
                return true;
            }

            return false;
        }

        private void InitOrderParameter()
        {
            #region �̻�����
            Merchant.NonceStr = Util.GenerateNonceStr();
            GatewayData.Add(Constant.APPID, Merchant.AppId);
            GatewayData.Add(Constant.MCH_ID, Merchant.MchId);
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.SIGN_TYPE, Merchant.SignType);
            GatewayData.Add(Constant.NOTIFY_URL, Merchant.NotifyUrl);
            GatewayData.Add(Constant.DEVICE_INFO, Constant.WEB);

            #endregion

            #region ��������

            GatewayData.Add(Constant.BODY, Order.Body);
            GatewayData.Add(Constant.OUT_TRADE_NO, Order.OutTradeNo);
            GatewayData.Add(Constant.FEE_TYPE, Order.FeeType);
            GatewayData.Add(Constant.TOTAL_FEE, Order.Amount * 100);
            GatewayData.Add(Constant.TIME_START, Order.TimeStart);
            GatewayData.Add(Constant.SPBILL_CREATE_IP, Order.SpbillCreateIp);

            if (!string.IsNullOrEmpty(Order.TradeType))
            {
                GatewayData.Add(Constant.TRADE_TYPE, Order.TradeType);
            }

            if (!string.IsNullOrEmpty(Order.Detail))
            {
                GatewayData.Add(Constant.DETAIL, Order.Detail);
            }

            if (!string.IsNullOrEmpty(Order.Attach))
            {
                GatewayData.Add(Constant.ATTACH, Order.Attach);
            }

            if (!string.IsNullOrEmpty(Order.TimeExpire))
            {
                GatewayData.Add(Constant.TIME_EXPIRE, Order.TimeExpire);
            }

            if (!string.IsNullOrEmpty(Order.GoodsTag))
            {
                GatewayData.Add(Constant.GOODS_TAG, Order.GoodsTag);
            }

            if (!string.IsNullOrEmpty(Order.ProductId))
            {
                GatewayData.Add(Constant.PRODUCT_ID, Order.ProductId);
            }

            if (!string.IsNullOrEmpty(Order.LimitPay))
            {
                GatewayData.Add(Constant.LIMIT_PAY, Order.LimitPay);
            }

            if (!string.IsNullOrEmpty(Order.OpenId))
            {
                GatewayData.Add(Constant.OPENID, Order.OpenId);
            }

            if (!string.IsNullOrEmpty(Order.SceneInfo))
            {
                GatewayData.Add(Constant.SCENE_INFO, Order.SceneInfo);
            }

            if (!string.IsNullOrEmpty(Order.AuthCode))
            {
                GatewayData.Add(Constant.AUTH_CODE, Order.AuthCode);
            }

            #endregion

            GatewayData.Add(Constant.SIGN, BuildSign());
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
            ReadNotify<Notify>();
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
