using ICanPay.Core;
using System;
using System.Threading.Tasks;

namespace ICanPay.Wechatpay
{
    /// <summary>
    /// ΢��֧������
    /// </summary>
    public sealed class WechatpayGataway : GatewayBase, IPaymentQRCode, IQueryNow, IPaymentApp, IPaymentUrl
    {

        #region ˽���ֶ�

        private Merchant merchant;
        private const string queryGatewayUrl = "https://api.mch.weixin.qq.com/pay/orderquery";

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

        public override string GatewayUrl => "https://api.mch.weixin.qq.com/pay/unifiedorder";

        public new Merchant Merchant => merchant;

        public new Order Order => (Order)base.Order;

        public new Notify Notify => (Notify)base.Notify;

        #endregion

        #region ����

        protected override async Task<bool> CheckNotifyDataAsync()
        {
            if (IsSuccessResult())
            {
                ReadNotifyOrder();
                return true;
            }

            return false;
        }

        public string BuildPaymentQRCode()
        {
            return null;//GetWeixinPaymentUrl(UnifiedOrder());
        }

        public string BuildPaymentApp()
        {
            UnifiedOrder();
            InitAppParameter();
            return GatewayData.ToUrlEncode();
        }

        public string BuildPaymentUrl()
        {
            UnifiedOrder();
            return Notify.MWebUrl;
        }

        protected override void InitOrderParameter()
        {
            base.InitOrderParameter();

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
            GatewayData.Add(Constant.TRADE_TYPE, Order.TradeType);
            GatewayData.Add(Constant.SPBILL_CREATE_IP, Order.SpbillCreateIp);

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

            #endregion

            GatewayData.Add(Constant.SIGN, BuildSign());
        }

        protected override void SupplementaryAppParameter()
        {
            if (!string.IsNullOrEmpty(Order.SceneInfo))
            {
                GatewayData.Add(Constant.SCENE_INFO, Order.SceneInfo);
            }

            Order.TradeType = Constant.APP;
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        protected override void SupplementaryWebParameter()
        {
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        protected override void SupplementaryWapParameter()
        {
            if (!string.IsNullOrEmpty(Order.SceneInfo))
            {
                GatewayData.Add(Constant.SCENE_INFO, Order.SceneInfo);
            }
            else
            {
                throw new ArgumentNullException("SceneInfo ��������Ϊ��");
            }

            Order.TradeType = Constant.MWEB;
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        protected override void SupplementaryScanParameter()
        {
            Order.SpbillCreateIp = HttpUtil.LocalIpAddress.ToString();
        }

        public bool QueryNow()
        {
            return CheckQueryResult(QueryOrder());
        }

        /// <summary>
        /// ͳһ�µ�
        /// </summary>
        /// <returns></returns>
        private void UnifiedOrder()
        {
            InitOrderParameter();
            string result = HttpUtil
                .PostAsync(GatewayUrl, GatewayData.ToXml())
                .GetAwaiter()
                .GetResult();
            ReadReturnResult(result);
        }

        private string QueryOrder()
        {
            InitQueryOrderParameter();
            return HttpUtil
                .PostAsync(queryGatewayUrl, GatewayData.ToXml())
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// ��ȡ֪ͨ�еĶ������������
        /// </summary>
        private void ReadNotifyOrder()
        {
            Order.OutTradeNo = GatewayData.GetStringValue(Constant.OUT_TRADE_NO);
            Order.Amount = GatewayData.GetIntValue(Constant.TOTAL_FEE) * 0.01;
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
        /// ���΢��֧����URL
        /// </summary>
        /// <param name="resultXml">�����������ص�����</param>
        /// <returns></returns>
        private string GetWeixinPaymentUrl(string resultXml)
        {
            GatewayData.FromXml(resultXml);
            if (IsSuccessResult())
            {
                return GatewayData.GetStringValue(Constant.CODE_URL);
            }

            return string.Empty;
        }

        /// <summary>
        /// �Ƿ����ѳɹ�֧����֧��֪ͨ
        /// </summary>
        /// <returns></returns>
        private bool IsSuccessResult()
        {
            if (string.Compare(GatewayData.GetStringValue(Constant.RETURN_CODE), SUCCESS) == 0 &&
                string.Compare(GatewayData.GetStringValue(Constant.RESULT_CODE), SUCCESS) == 0 &&
                string.Compare(GatewayData.GetStringValue(Constant.SIGN), BuildSign()) == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// �Ƿ����ѳɹ��ķ���
        /// </summary>
        /// <returns></returns>
        private void IsSuccessReturn()
        {
            if (Notify.ReturnCode == FAIL)
            {
                throw new Exception(Notify.ReturnMsg);
            }
        }

        /// <summary>
        /// ����ѯ���
        /// </summary>
        /// <param name="resultXml">��ѯ�����XML</param>
        /// <returns></returns>
        private bool CheckQueryResult(string resultXml)
        {
            GatewayData.FromXml(resultXml);
            if (IsSuccessResult())
            {
                if (string.Compare(Order.OutTradeNo, GatewayData.GetStringValue(Constant.OUT_TRADE_NO)) == 0 &&
                   Order.Amount == GatewayData.GetIntValue(Constant.TOTAL_FEE) / 100.0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ��ʼ����ѯ��������
        /// </summary>
        private void InitQueryOrderParameter()
        {
            GatewayData.Add(Constant.MCH_ID, Merchant.MchId);
            GatewayData.Add(Constant.OUT_TRADE_NO, Order.OutTradeNo);
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.SIGN, BuildSign());
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
            GatewayData.Add(Constant.PREPAYID, "23");
            GatewayData.Add(Constant.PACKAGE, "Sign=WXPay");
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.TIMESTAMP, DateTime.Now.ToTimeStamp());
            GatewayData.Add(Constant.SIGN, BuildSign());
        }

        public override void WriteSuccessFlag()
        {
            GatewayData.Add(Constant.RETURN_CODE, SUCCESS);
            HttpUtil.Write(GatewayData.ToXml());
        }

        #endregion
    }
}
