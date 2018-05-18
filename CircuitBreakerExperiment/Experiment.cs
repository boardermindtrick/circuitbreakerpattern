using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircuitBreakerExperiment
{
    public abstract class CircuitBreakerState
    {
        protected readonly CircuitBreaker circuitBreaker;

        protected CircuitBreakerState(CircuitBreaker circuitBreaker)
        {
            this.circuitBreaker = circuitBreaker;
        }

        public virtual CircuitBreaker ProtectedCodeIsAboutToBeCalled()
        {
            return this.circuitBreaker;
        }

        public virtual void ProtectedCodeHasBeenCalled()
        {

        }

        public virtual void ActUponException(Exception e)
        {
            circuitBreaker.IncreaseFailureCount();
        }

        public virtual CircuitBreakerState Update()
        {
            return this;
        }

    }

    public class OpenState : CircuitBreakerState
    {
        private readonly DateTime openDateTime;
        public OpenState (CircuitBreaker circuitBreaker) : base(circuitBreaker)
        {
            openDateTime = DateTime.UtcNow;
        }

        public override CircuitBreaker ProtectedCodeIsAboutToBeCalled()
        {
            return base.ProtectedCodeIsAboutToBeCalled();
            this.Update();
            return base.circuitBreaker;
        }

        public override CircuitBreakerState Update()
        {
            return base.Update();
            if(DateTime.UtcNow>= openDateTime + base.circuitBreaker.TimeOut)
            {
                return circuitBreaker.MoveToHalfOpenState();
            }
            return this;
        }
    }

    public class HalfOpenState : CircuitBreakerState
    {
        public HalfOpenState(CircuitBreaker circuitBreaker): base(circuitBreaker)
        {

        }
        public override void ActUponException(Exception e)
        {
            base.ActUponException(e);
            circuitBreaker.MoveToOpenState();
        }

        public override void ProtectedCodeHasBeenCalled()
        {
            base.ProtectedCodeHasBeenCalled();
            circuitBreaker.MoveToCloseState();
        }
    }

    public class ClosedState : CircuitBreakerState
    {
        public ClosedState(CircuitBreaker circuitBreaker) : base(circuitBreaker)
        {
            circuitBreaker.ResetFailureCount();
        }

        public override void ActUponException(Exception e)
        {
            base.ActUponException(e);
            if(circuitBreaker.IsThresholdReached())
            {
                circuitBreaker.MoveToOpenState();
            }
        }
    }

    public class CircuitBreaker
    {
        private readonly object Monitor = new object();
        private CircuitBreakerState state;
        public int Failures { get; private set; }
        public int Threshold { get; private set; }
        public TimeSpan TimeOut { get; private set; }
        public CircuitBreaker(int threshold, TimeSpan timeout)
        {
            if(threshold < 1)
            {
                throw new ArgumentOutOfRangeException("threshold", "Threshold should be greater than 0");
            }

            if(timeout.Minutes < 1)
            {
                throw new ArgumentOutOfRangeException("timeout", "Timeout should be greater than 0");
            }

            Threshold = threshold;
            TimeOut = timeout;
            MoveToCloseState();
        }

        public bool IsClosed
        {
            get
            {
                return state.Update() is ClosedState;
            }
        }

        public bool IsOpen
        {
            get
            {
                return state.Update() is OpenState;
            }
        }

        public bool IsHalfOpen
        {
            get
            {
                return state.Update() is HalfOpenState;
            }
        }

        internal CircuitBreakerState MoveToCloseState()
        {
            state = new ClosedState(this);
            return state;
        }

        internal CircuitBreakerState MoveToOpenState()
        {
            state = new OpenState(this);
            return state;
        }

        internal CircuitBreakerState MoveToHalfOpenState()
        {
            state = new HalfOpenState(this);
            return state;
        }

        internal void IncreaseFailureCount()
        {
            Failures++;
        }

        internal void ResetFailureCount()
        {
            Failures = 0;
        }

        public bool IsThresholdReached()
        {
            return Failures >= Threshold;
        }

        private Exception exceptionFromLastAttemptCall = null;
        public Exception GetExceptionFromLastAttemptCall()
        {
            return exceptionFromLastAttemptCall;
        }

        public CircuitBreaker AttemptCall(Action protectedCode)
        {
            this.exceptionFromLastAttemptCall = null;
            lock(Monitor)
            {
                state.ProtectedCodeIsAboutToBeCalled();
                if(state is OpenState)
                {
                    return this;
                }
            }
            try
            {
                protectedCode();
            }
            catch(Exception e)
            {
                this.exceptionFromLastAttemptCall = e;
                lock(Monitor)
                {
                    state.ActUponException(e);
                }
                return this;
            }

            lock(Monitor)
            {
                state.ProtectedCodeHasBeenCalled();
            }
            return this;
        }

        public void Close()
        {
            lock(Monitor)
            {
                MoveToCloseState();
            }
        }

        public void Open()
        {
            lock(Monitor)
            {
                MoveToOpenState();
            }
        }
    }
}
