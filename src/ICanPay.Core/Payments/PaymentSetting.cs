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
                        if (gateway is IAppPayment appPayment)
                        {
                            return appPayment.BuildAppPayment();
                        }
                    }
                    break;
                case GatewayTradeType.Wap:
                    {
                        if (gateway is IUrlPayment urlPayment)
                        {
                            HttpUtil.Redirect(urlPayment.BuildUrlPayment());
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Web:
                    {
                        if (gateway is IFormPayment formPayment)
                        {
                            HttpUtil.Write(formPayment.BuildFormPayment());
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Scan:
                    {
                        if (gateway is IScanPayment scanPayment)
                        {
                            return scanPayment.BuildScanPayment();
                        }
                    }
                    break;
                case GatewayTradeType.Public:
                    {
                        if (gateway is IPublicPayment publicPayment)
                        {
                            return publicPayment.BuildPublicPayment();
                        }
                    }
                    break;
                case GatewayTradeType.Barcode:
                    {
                        if (gateway is IBarcodePayment barcodePayment)
                        {
                            barcodePayment.BuildBarcodePayment();
                            return null;
                        }
                    }
                    break;
                case GatewayTradeType.Applet:
                    {
                        if (gateway is IAppletPayment appletPayment)
                        {
                            return appletPayment.BuildAppletPayment();
                        }
                    }
                    break;
                default:
                    break;
            }

            throw new NotSupportedException(gateway.GatewayType + " û��ʵ��֧���ӿ�");
        }

        #endregion
    }
}
