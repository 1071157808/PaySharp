using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ICanPay.Core
{
    /// <summary>
    /// ���ط��ص�֧��֪ͨ���ݵĽ���
    /// </summary>
    public class PaymentNotify
    {
        #region ˽���ֶ�

        private ICollection<GatewayBase> gatewayList;

        #endregion

        #region ���캯��

        /// <summary>
        /// ��ʼ��֧��֪ͨ
        /// </summary>
        /// <param name="gatewayList">������֤֧�����ط������ݵ������б�</param>
        public PaymentNotify(ICollection<GatewayBase> gatewayList)
        {
            this.gatewayList = gatewayList;
        }

        #endregion

        #region �¼�

        /// <summary>
        /// ���ط��ص�֧��֪ͨ��֤ʧ��ʱ����
        /// </summary>
        public event Action<object, PaymentFailedEventArgs> PaymentFailed;

        /// <summary>
        /// ���ط��ص�֧��֪ͨ��֤�ɹ�ʱ����
        /// </summary>
        public event Action<object, PaymentSucceedEventArgs> PaymentSucceed;

        /// <summary>
        /// ����֪ͨ��Ϣ�������޷�ʶ��ʱ����
        /// </summary>
        public event Action<object, UnknownGatewayEventArgs> UnknownGateway;

        #endregion

        #region ����

        protected virtual void OnPaymentFailed(PaymentFailedEventArgs e) => PaymentFailed?.Invoke(this, e);

        protected virtual void OnPaymentSucceed(PaymentSucceedEventArgs e) => PaymentSucceed?.Invoke(this, e);

        protected virtual void OnUnknownGateway(UnknownGatewayEventArgs e) => UnknownGateway?.Invoke(this, e);

        /// <summary>
        /// ���ղ���֤���ص�֧��֪ͨ
        /// </summary>
        public async Task ReceivedAsync()
        {
            GatewayBase gateway = NotifyProcess.GetGateway(gatewayList);
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