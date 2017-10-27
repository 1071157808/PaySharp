using System;
using System.Threading.Tasks;

namespace ICanPay.Core
{
    /// <summary>
    /// ���ط��ص�֧��֪ͨ���ݵĽ���
    /// </summary>
    public class PaymentNotify
    {
        #region ˽���ֶ�

        private IGateways gateways;

        #endregion

        #region ���캯��

        /// <summary>
        /// ��ʼ��֧��֪ͨ
        /// </summary>
        /// <param name="gateways">������֤֧�����ط������ݵ������б�</param>
        public PaymentNotify(IGateways gateways)
        {
            this.gateways = gateways;
        }

        #endregion

        #region �¼�

        /// <summary>
        /// �����첽���ص�֧��֪ͨ��֤ʧ��ʱ����
        /// </summary>
        public event Action<object, PaymentFailedEventArgs> PaymentFailed;

        /// <summary>
        /// �����첽���ص�֧��֪ͨ��֤�ɹ�ʱ����
        /// </summary>
        public event Action<object, PaymentSucceedEventArgs> PaymentSucceed;

        /// <summary>
        /// �����첽���ص�֧��֪ͨ�޷�ʶ��ʱ����
        /// </summary>
        public event Action<object, UnknownGatewayEventArgs> UnknownGateway;

        #endregion

        #region ����

        private void OnPaymentFailed(PaymentFailedEventArgs e) => PaymentFailed?.Invoke(this, e);

        private void OnPaymentSucceed(PaymentSucceedEventArgs e) => PaymentSucceed?.Invoke(this, e);

        private void OnUnknownGateway(UnknownGatewayEventArgs e) => UnknownGateway?.Invoke(this, e);

        /// <summary>
        /// ���ղ���֤���ص�֧��֪ͨ
        /// </summary>
        public async Task ReceivedAsync()
        {
            GatewayBase gateway = NotifyProcess.GetGateway(gateways);
            if (gateway.GatewayType != GatewayType.None)
            {
                if (await gateway.ValidateNotifyAsync())
                {
                    OnPaymentSucceed(new PaymentSucceedEventArgs(gateway));
                    gateway.WriteSuccessFlag();
                }
                else
                {
                    OnPaymentFailed(new PaymentFailedEventArgs(gateway));
                    gateway.WriteFailureFlag();
                }
            }
            else
            {
                OnUnknownGateway(new UnknownGatewayEventArgs(gateway));
            }
        }

        #endregion

    }
}