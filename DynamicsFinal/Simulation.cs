using System;

namespace DynamicsFinal;

public readonly struct StateVector {
    public readonly double Theta1, Theta2, Omega1, Omega2;

    static double NormalizeAngle(double angle) {
        angle %= Math.Tau;
        return angle < 0 ? Math.Tau + angle : angle;
    }
    public StateVector(double theta1, double theta2, double omega1, double omega2) {
        Theta1 = NormalizeAngle(theta1);
        Theta2 = NormalizeAngle(theta2);
        Omega1 = omega1;
        Omega2 = omega2;
    }
    public static StateVector operator +(StateVector v1, StateVector v2) => new(v1.Theta1 + v2.Theta1, v1.Theta2 + v2.Theta2,
        v1.Omega1 + v2.Omega1, v1.Omega2 + v2.Omega2);
    public static StateVector operator *(StateVector v, double d) => new(v.Theta1 * d, v.Theta2 * d, v.Omega1 * d, v.Omega2 * d);
    public static StateVector operator *(double d, StateVector v) => v * d;
}

public class Simulation {
    public readonly double L1, L2, M1, M2, G;
    public Simulation(double l1, double l2, double m1, double m2, double g) {
        L1 = l1;
        L2 = l2;
        M1 = m1;
        M2 = m2;
        G = g;
    }

    //RK4: https://en.wikipedia.org/wiki/Runge%E2%80%93Kutta_methods
    public StateVector Step(StateVector current, double stepSize) {
        StateVector k1 = Derivative(current);
        StateVector k2 = Derivative(current + stepSize / 2 * k1);
        StateVector k3 = Derivative(current + stepSize / 2 * k2);
        StateVector k4 = Derivative(current + stepSize * k3);
        return current + stepSize / 6 * (k1 + 2 * k2 + 2 * k3 + k4);
    }
    
    StateVector Derivative(StateVector vector) {
        double sinT1 = Math.Sin(vector.Theta1);
        double sinT2 = Math.Sin(vector.Theta2);
        double cosT1 = Math.Cos(vector.Theta1);
        double cosT2 = Math.Cos(vector.Theta2);
        double sinT1MinusT2 = Math.Sin(vector.Theta1 - vector.Theta2);
        double aDet = L1 * L2 / (M1 + M2) * (sinT1MinusT2 * sinT1MinusT2 * M2 + M1);
        double gFraction = M2 / (M1 + M2);
        double a2Det = -L2 * sinT1MinusT2 * gFraction *
            ((L1 * vector.Omega1 * vector.Omega1 * sinT1 * sinT2 +
              gFraction * L2 * vector.Omega2 * vector.Omega2 * sinT2 * sinT2)
             + cosT2 * (G + L1 * vector.Omega1 * vector.Omega1 * cosT1 +
                        gFraction * L2 * vector.Omega2 * vector.Omega2 * cosT2)) - L2 * M1 / (M1 + M2) *
            (sinT1 * (G + L1 * vector.Omega1 * vector.Omega1 * cosT1 +
                      gFraction * L2 * vector.Omega2 * vector.Omega2 * cosT2) - cosT1 *
                (L1 * vector.Omega1 * vector.Omega1 * sinT1 + gFraction * L2 * vector.Omega2 * vector.Omega2 * sinT2));
        double a3Det = L1 * sinT1MinusT2 *
                       (cosT1 * (G + L1 * vector.Omega1 * vector.Omega1 * cosT1 +
                                 gFraction * L2 * vector.Omega2 * vector.Omega2 * cosT2) +
                        sinT1 *
                        (L1 * vector.Omega1 * vector.Omega1 * sinT1 + gFraction * L2 * vector.Omega2 * vector.Omega2 * sinT2));
        return new StateVector(vector.Omega1, vector.Omega2, a2Det / aDet, a3Det / aDet);
    }
}