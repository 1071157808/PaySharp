using System;

namespace ICanPay.Core
{
    /// <summary>
    /// ������Ҫ֧���Ķ��������ݣ�����֧������URL��ַ��HTML��
    /// </summary>
    public class PaymentSetting
    {

        #region �ֶ�

        private GatewayBase gateway;

        #endregion

        #region ���캯��

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="gateway">����</param>
        public PaymentSetting(GatewayBase gateway)
        {
            this.gateway = gateway;
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="gateway">����</param>
        /// <param name="order">����</param>
        public PaymentSetting(GatewayBase gateway, IOrder order)
            : this(gateway)
        {
            this.gateway.Order = order;
        }

        #endregion

        #region ����

        /// <summary>
        /// ��������
        /// </summary>
        public IOrder Order
        {
            get
            {
                return gateway.Order;
            }

            set
            {
                gateway.Order = value;
            }
        }

        public bool CanQueryNotify
        {
            get
            {
                if (gateway is IQueryUrl || gateway is IQueryForm)
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanQueryNow
        {
            get
            {
                return gateway is IQueryNow;
            }
        }

        #endregion

        #region ����

        /// <summary>
        /// ����������֧��Url��Form������ά�롣
        /// </summary>
        /// <remarks>
        /// ����������Ƕ�����Url��Form������ת����Ӧ����֧��������Ƕ�ά�뽫�����ά��ͼƬ��
        /// </remarks>
        public string Payment()
        {
            switch (gateway.GatewayTradeType)
            {
                case GatewayTradeType.App:
                    {
                        if (gateway is IPaymentApp paymentApp)
                        {
                            return paymentApp.BuildPaymentApp();
                        }
                    }
                    break;
                case GatewayTradeType.Wap:
                    {
                        if (gateway is IPaymentUrl paymentUrl)
                        {
                            HttpUtil.Redirect(paymentUrl.BuildPaymentUrl());
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Web:
                    {
                        if (gateway is IPaymentForm paymentForm)
                        {
                            HttpUtil.Write(paymentForm.BuildPaymentForm());
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Scan:
                    {
                        if (gateway is IPaymentScan paymentScan)
                        {
                            return paymentScan.BuildPaymentScan();
                        }
                    }
                    break;
                case GatewayTradeType.Public:
                    {
                        if (gateway is IPaymentPublic paymentPublic)
                        {
                            return paymentPublic.BuildPaymentPublic();
                        }
                    }
                    break;
                case GatewayTradeType.Barcode:
                    {
                        if (gateway is IPaymentBarcode paymentBarcode)
                        {
                            return paymentBarcode.BuildPaymentBarcode();
                        }
                    }
                    break;
                default:
                    break;
            }

            throw new NotSupportedException(gateway.GatewayType + " û��ʵ��֧���ӿ�");
        }

        /// <summary>
        /// ��ѯ�����������Ĳ�ѯ֪ͨ����ͨ����֧��֪ͨһ������ʽ���ء��ô�������֪ͨһ���ķ������ܲ�ѯ���������ݡ�
        /// </summary>
        public void QueryNotify()
        {
            if (gateway is IQueryUrl queryUrl)
            {
                HttpUtil.Redirect(queryUrl.BuildQueryUrl());
                return;
            }

            if (gateway is IQueryForm queryForm)
            {
                HttpUtil.Write(queryForm.BuildQueryForm());
                return;
            }

            throw new NotSupportedException(gateway.GatewayType + " û��ʵ�� IQueryUrl �� IQueryForm ��ѯ�ӿ�");
        }

        /// <summary>
        /// ��ѯ������������ö����Ĳ�ѯ���
        /// </summary>
        /// <returns></returns>
        public bool QueryNow()
        {
            if (gateway is IQueryNow queryNow)
            {
                return queryNow.QueryNow();
            }

            throw new NotSupportedException(gateway.GatewayType + " û��ʵ�� IQueryNow ��ѯ�ӿ�");
        }

        #endregion

    }
}
