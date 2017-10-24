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

        #endregion

        #region ����

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
        private void ValidateParameter(object instance)
        {
            var validationContext = new ValidationContext(instance, new Dictionary<object, object>
            {
                { "GatewayTradeType", GatewayTradeType }
            });
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(instance, validationContext, results, true);

            if (!isValid)
            {
                throw new Exception(results[0].ErrorMessage);
            }
        }

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

        /// <summary>
        /// ��ʼ����������
        /// </summary>
        protected virtual void InitOrderParameter()
        {
            SupplementaryParameter();
        }

        /// <summary>
        /// ���䲻֧ͬ�����͵�ȱ�ٲ���
        /// </summary>
        private void SupplementaryParameter()
        {
            switch (GatewayTradeType)
            {
                case GatewayTradeType.App:
                    {
                        SupplementaryAppParameter();
                    }
                    break;
                case GatewayTradeType.Wap:
                    {
                        SupplementaryWapParameter();
                    }
                    break;
                case GatewayTradeType.Web:
                    {
                        SupplementaryWebParameter();
                    }
                    break;
                case GatewayTradeType.Scan:
                    {
                        SupplementaryScanParameter();
                    }
                    break;
                case GatewayTradeType.Public:
                    {
                        SupplementaryPublicParameter();
                    }
                    break;
                case GatewayTradeType.Barcode:
                    {
                        SupplementaryBarcodeParameter();
                    }
                    break;
                case GatewayTradeType.Applet:
                    {
                        SupplementaryAppletParameter();
                    }
                    break;
                default:
                    break;
            }

            ValidateParameter(Merchant);
            ValidateParameter(Order);
        }

        /// <summary>
        /// �����ƶ�֧����ȱ�ٲ���
        /// </summary>
        protected abstract void SupplementaryAppParameter();

        /// <summary>
        /// ���������վ֧����ȱ�ٲ���
        /// </summary>
        protected abstract void SupplementaryWebParameter();

        /// <summary>
        /// �����ֻ���վ֧����ȱ�ٲ���
        /// </summary>
        protected abstract void SupplementaryWapParameter();

        /// <summary>
        /// ����ɨ��֧����ȱ�ٲ���
        /// </summary>
        protected abstract void SupplementaryScanParameter();

        /// <summary>
        /// ���乫�ں�֧����ȱ�ٲ���
        /// </summary>
        protected virtual void SupplementaryPublicParameter()
        {
        }

        /// <summary>
        /// ��������֧����ȱ�ٲ���
        /// </summary>
        protected abstract void SupplementaryBarcodeParameter();

        /// <summary>
        /// ����С����֧����ȱ�ٲ���
        /// </summary>
        protected virtual void SupplementaryAppletParameter()
        {
        }

        /// <summary>
        /// ��ȡ֪ͨ
        /// </summary>
        protected void ReadNotify<T>() where T : INotify
        {
            Task.Run(() =>
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
            })
            .GetAwaiter()
            .GetResult();
        }

        #endregion

    }
}
