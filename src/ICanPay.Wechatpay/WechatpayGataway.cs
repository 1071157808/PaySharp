using ICanPay.Core;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ICanPay.Wechatpay
{
    /// <summary>
    /// ΢��֧������
    /// </summary>
    public sealed class WechatpayGataway : GatewayBase, IPaymentQRCode, IQueryNow, IPaymentApp
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
            return GetWeixinPaymentUrl(CreateOrder());
        }

        private string CreateOrder()
        {
            InitOrderParameter();
            return Util
                .PostAsync(queryGatewayUrl, GatewayData.ToXml())
                .GetAwaiter()
                .GetResult();
        }

        public bool QueryNow()
        {
            return CheckQueryResult(QueryOrder());
        }

        private string QueryOrder()
        {
            InitQueryOrderParameter();
            return Util
                .PostAsync(queryGatewayUrl, GatewayData.ToXml())
                .GetAwaiter()
                .GetResult();
        }

        protected override void InitOrderParameter()
        {
            base.InitOrderParameter();

            #region �̻�����
            Merchant.NonceStr = GenerateNonceStr();
            GatewayData.Add(Constant.APPID, Merchant.AppId);
            GatewayData.Add(Constant.MCH_ID, Merchant.MchId);
            GatewayData.Add(Constant.NONCE_STR, Merchant.NonceStr);
            GatewayData.Add(Constant.SIGN_TYPE, Merchant.SignType);
            GatewayData.Add(Constant.NOTIFY_URL, Merchant.NotifyUrl);
            if (!string.IsNullOrEmpty(Merchant.DeviceInfo))
            {
                GatewayData.Add(Constant.DEVICE_INFO, Merchant.DeviceInfo);
            }

            #endregion

            #region ��������

            GatewayData.Add(Constant.BODY, Order.Body);
            GatewayData.Add(Constant.OUT_TRADE_NO, Order.OutTradeNo);
            GatewayData.Add(Constant.FEE_TYPE, Order.FeeType);
            GatewayData.Add(Constant.TOTAL_FEE, (Order.Amount * 100).ToString());
            GatewayData.Add(Constant.TIME_START, Order.TimeStart);
            GatewayData.Add(Constant.TRADE_TYPE, Constant.APP);
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

            if (!string.IsNullOrEmpty(Order.SceneInfo))
            {
                GatewayData.Add(Constant.SCENE_INFO, Order.SceneInfo);
            }

            #endregion

            Merchant.Sign = BuildSign();
            GatewayData.Add(Constant.SIGN, Merchant.Sign);    // ǩ����Ҫ��������ã�����ȱ�ٲ�����
        }

        protected override void SupplementaryAppParameter()
        {
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        protected override void SupplementaryWebParameter()
        {
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        protected override void SupplementaryWapParameter()
        {
            Order.SpbillCreateIp = HttpUtil.RemoteIpAddress.ToString();
        }

        protected override void SupplementaryScanParameter()
        {
            Order.SpbillCreateIp = HttpUtil.LocalIpAddress.ToString();
        }

        public string BuildPaymentApp()
        {
            string result = CreateOrder();
            ReadReturnResult(result);
            return null;
        }

        /// <summary>
        /// ��������ַ���
        /// </summary>
        /// <returns></returns>
        private string GenerateNonceStr()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
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
            ClearGatewayData();
            GatewayData.FromXml(result);
            IsSuccessReturn();
        }

        /// <summary>
        /// ���ǩ��
        /// </summary>
        /// <returns></returns>
        private string BuildSign()
        {
            StringBuilder signBuilder = new StringBuilder();
            foreach (var item in GatewayData.Values)
            {
                // ��ֵ�Ĳ�����sign����������ǩ��
                if (string.Compare(Constant.SIGN, item.Key) != 0)
                {
                    signBuilder.AppendFormat("{0}={1}&", item.Key, item.Value);
                }
            }

            signBuilder.Append("key=" + Merchant.Key);
            return Util.GetMD5(signBuilder.ToString());
        }

        /// <summary>
        /// ���΢��֧����URL
        /// </summary>
        /// <param name="resultXml">�����������ص�����</param>
        /// <returns></returns>
        private string GetWeixinPaymentUrl(string resultXml)
        {
            // ��Ҫ�����֮ǰ���������Ĳ����������Խ��յ��Ĳ�����ɸ��š�
            ClearGatewayData();
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
        private bool IsSuccessReturn()
        {
            if (string.Compare(GatewayData.GetStringValue(Constant.RETURN_CODE), SUCCESS) == 0)
            {
                return true;
            }

            throw new Exception(GatewayData.GetStringValue(Constant.RETURN_MSG));
        }

        /// <summary>
        /// ����ѯ���
        /// </summary>
        /// <param name="resultXml">��ѯ�����XML</param>
        /// <returns></returns>
        private bool CheckQueryResult(string resultXml)
        {
            // ��Ҫ�����֮ǰ��ѯ�����Ĳ����������Խ��յ��Ĳ�����ɸ��š�
            ClearGatewayData();
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
            GatewayData.Add(Constant.SIGN, BuildSign());    // ǩ����Ҫ��������ã�����ȱ�ٲ�����
        }

        /// <summary>
        /// ������ص�����
        /// </summary>
        private void ClearGatewayData()
        {
            GatewayData.Clear();
        }

        /// <summary>
        /// ��ʼ����ʾ�ѳɹ����յ�֧��֪ͨ������
        /// </summary>
        private void InitProcessSuccessParameter()
        {
            GatewayData.Add(Constant.RETURN_CODE, SUCCESS);
        }

        public override void WriteSuccessFlag()
        {
            // ��Ҫ�����֮ǰ���յ���֪ͨ�Ĳ��������������ɱ�־�ɹ����յ�֪ͨ��XML��ɸ��š�
            ClearGatewayData();
            InitProcessSuccessParameter();
            HttpUtil.Write(GatewayData.ToXml());
        }

        #endregion
    }
}
