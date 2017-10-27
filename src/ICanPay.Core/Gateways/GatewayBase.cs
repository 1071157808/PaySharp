using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ICanPay.Core
{
    /// <summary>
    /// ֧�����صĳ������
    /// </summary>
    public abstract class GatewayBase
    {
        #region �����ֶ�

        public const string TRUE = "true";
        public const string FALSE = "false";
        public const string SUCCESS = "success";
        public const string FAILURE = "failure";
        public const string FAIL = "FAIL";
        public const string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

        #endregion

        #region ˽���ֶ�

        private GatewayData gatewayData;

        #endregion

        #region ���캯��

        /// <summary>
        /// ���캯��
        /// </summary>
        protected GatewayBase()
            : this(new GatewayData())
        {
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="merchant">�̻�����</param>
        protected GatewayBase(IMerchant merchant)
            : this(new GatewayData())
        {
            Merchant = merchant;
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="gatewayData">��������</param>
        protected GatewayBase(GatewayData gatewayData)
        {
            this.gatewayData = gatewayData;
        }

        #endregion

        #region ����

        /// <summary>
        /// ��������
        /// </summary>
        public IOrder Order { get; set; }

        /// <summary>
        /// �̻�����
        /// </summary>
        public IMerchant Merchant { get; set; }

        /// <summary>
        /// ֪ͨ����
        /// </summary>
        public INotify Notify { get; set; }

        /// <summary>
        /// ֧�����ص�����
        /// </summary>
        public abstract GatewayType GatewayType { get; }

        /// <summary>
        /// ֧�����صĵ�ַ
        /// </summary>
        public abstract string GatewayUrl { get; set; }

        /// <summary>
        /// ֧�����صĽ�������
        /// </summary>
        public GatewayTradeType GatewayTradeType { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        public GatewayData GatewayData
        {
            get
            {
                return gatewayData;
            }
            set
            {
                gatewayData = value;
            }
        }

        /// <summary>
        /// �Ƿ�ɹ�֧��
        /// </summary>
        protected abstract bool IsSuccessPay { get; }

        /// <summary>
        /// �Ƿ�ȴ�֧��
        /// </summary>
        protected abstract bool IsWaitPay { get; }

        #endregion

        #region ����

        #region ���󷽷�

        /// <summary>
        /// �������ط��ص�֪ͨ��ȷ�϶����Ƿ�֧���ɹ�
        /// </summary>
        protected abstract Task<bool> CheckNotifyDataAsync();

        /// <summary>
        /// �����յ�֧������֪ͨ����֤����ʱ����֧������Ҫ���ʽ�����ʾ�ɹ����յ�����֪ͨ���ַ���
        /// </summary>
        public virtual void WriteSuccessFlag()
        {
            HttpUtil.Write(SUCCESS);
        }

        /// <summary>
        /// �����յ�֧������֪ͨ����֤����ʱ����֧������Ҫ���ʽ�����ʾʧ�ܽ��յ�����֪ͨ���ַ���
        /// </summary>
        public virtual void WriteFailureFlag()
        {
            HttpUtil.Write(FAILURE);
        }

        #endregion

        #region ˽�з���

        /// <summary>
        /// ��֤�����Ƿ�֧���ɹ�
        /// </summary>
        internal async Task<bool> ValidateNotifyAsync()
        {
            if (await CheckNotifyDataAsync())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// ��֤����
        /// </summary>
        /// <param name="instance">��֤����</param>
        protected void ValidateParameter(object instance)
        {
            var validationContext = new ValidationContext(instance, new Dictionary<object, object>
            {
                { "GatewayTradeType", GatewayTradeType }
            });
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(instance, validationContext, results, true);

            if (!isValid)
            {
                throw new ArgumentNullException(results[0].ErrorMessage);
            }
        }

        /// <summary>
        /// ��ȡ֪ͨ
        /// </summary>
        protected void ReadNotify<T>() where T : INotify
        {
            var type = typeof(T);
            var notify = Activator.CreateInstance(type);
            var properties = type.GetProperties();

            foreach (var item in properties)
            {
                string key = item
                    .GetCustomAttributesData()[0]
                    .NamedArguments[0]
                    .TypedValue
                    .Value
                    .ToString();
                object value = GatewayData.GetValue(key);

                if (value != null)
                {
                    item.SetValue(notify, Convert.ChangeType(value, item.PropertyType));
                }
            }

            Notify = (INotify)notify;
        }

        /// <summary>
        /// �첽��ȡ֪ͨ
        /// </summary>
        protected async Task ReadNotifyAsync<T>() where T : INotify
        {
            await Task.Run(() => ReadNotify<T>());
        }

        protected void OnPaymentFailed(PaymentFailedEventArgs e) => PaymentFailed?.Invoke(this, e);

        protected void OnPaymentSucceed(PaymentSucceedEventArgs e) => PaymentSucceed?.Invoke(this, e);

        #endregion

        #region ��������

        /// <summary>
        /// ֧��
        /// </summary>
        public string Payment()
        {
            switch (GatewayTradeType)
            {
                case GatewayTradeType.App:
                    {
                        if (this is IAppPayment appPayment)
                        {
                            return appPayment.BuildAppPayment();
                        }
                    }
                    break;
                case GatewayTradeType.Wap:
                    {
                        if (this is IUrlPayment urlPayment)
                        {
                            HttpUtil.Redirect(urlPayment.BuildUrlPayment());
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Web:
                    {
                        if (this is IFormPayment formPayment)
                        {
                            HttpUtil.Write(formPayment.BuildFormPayment());
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Scan:
                    {
                        if (this is IScanPayment scanPayment)
                        {
                            return scanPayment.BuildScanPayment();
                        }
                    }
                    break;
                case GatewayTradeType.Public:
                    {
                        if (this is IPublicPayment publicPayment)
                        {
                            return publicPayment.BuildPublicPayment();
                        }
                    }
                    break;
                case GatewayTradeType.Barcode:
                    {
                        if (this is IBarcodePayment barcodePayment)
                        {
                            barcodePayment.BuildBarcodePayment();
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Applet:
                    {
                        if (this is IAppletPayment appletPayment)
                        {
                            return appletPayment.BuildAppletPayment();
                        }
                    }
                    break;
                default:
                    break;
            }

            throw new NotSupportedException($"{GatewayType} û��ʵ�� {GatewayTradeType} �ӿ�");
        }

        /// <summary>
        /// ��ѯ
        /// </summary>
        public INotify Query()
        {
            if (this is IQuery query)
            {
                return query.BuildQuery();
            }

            throw new NotSupportedException($"{GatewayType} û��ʵ�� IQuery ��ѯ�ӿ�");
        }

        /// <summary>
        /// ����/�ر�
        /// </summary>
        public INotify Cancel()
        {
            if (this is ICancel cancel)
            {
                return cancel.BuildCancel();
            }

            throw new NotSupportedException($"{GatewayType} û��ʵ�� ICancel ��ѯ�ӿ�");
        }

        #endregion

        #endregion

        #region �¼�

        /// <summary>
        /// ����ͬ�����ص�֧��֪ͨ��֤ʧ��ʱ����,Ŀǰ���������֧��
        /// </summary>
        public event Action<object, PaymentFailedEventArgs> PaymentFailed;

        /// <summary>
        /// ����ͬ�����ص�֧��֪ͨ��֤�ɹ�ʱ����,Ŀǰ���������֧��
        /// </summary>
        public event Action<object, PaymentSucceedEventArgs> PaymentSucceed;

        #endregion
    }
}
