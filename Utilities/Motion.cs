using System;

/*
 * Motion.cpp
 * Copyright (c) 2015, ZHAW
 * All rights reserved.
 *
 *  Created on: 23.11.2015
 *      Author: Marcel Honegger
 */
 /**
 * This class keeps the motion values <code>position</code> and <code>velocity</code>, and
 * offers methods to increment these values towards a desired target position or velocity.
 * <br/>
 * To increment the current motion values, this class uses a simple 2nd order motion planner.
 * This planner calculates the motion to the target position or velocity with the various motion
 * phases, based on given limits for the profile velocity, acceleration and deceleration.
 * <br/>
 * The following figures show the different profile types used for planning a trajectory from
 * the current actual velocity to a given target velocity.
 * <img src="motion1.png" width="570" style="text-align:center"/>
 * <br/>
 * <div style="text-align:center"><b>The profile types used for planning a trajectory to a velocity</b></div>
 * The following figures show the different profile types used for planning a trajectory from
 * the current actual position and velocity to a given target position.
 * <img src="motion2.png" width="725" style="text-align:center"/>
 * <br/>
 * <div style="text-align:center"><b>The profile types used for planning a trajectory to a position</b></div>
 * <br/>
 * Note that the trajectory is calculated every time the motion state is incremented.
 * This allows to change the target position or velocity, as well as the limits for profile
 * velocity, acceleration and deceleration at any time.
 */

namespace PathPlaner
{
    namespace Motion
    {
        public static class GlobalMembers
        {
            public const float DEFAULT_LIMIT = 1.0f; // default value for limits
            public const float MINIMUM_LIMIT = 1.0e-9f; // smallest value allowed for limits
        }

        public class Motion
        {
            public double position; //*< The position value of this motion, given in [m] or [rad].
            public double velocity; //*< The velocity value of this motion, given in [m/s] or [rad/s].

            /**
             * Creates a <code>Motion</code> object.
             * The values for position, velocity and acceleration are set to 0.
             */
            public Motion()
            {

                position = 0.0;
                velocity = 0.0f;

                profileVelocity = GlobalMembers.DEFAULT_LIMIT;
                profileAcceleration = GlobalMembers.DEFAULT_LIMIT;
                profileDeceleration = GlobalMembers.DEFAULT_LIMIT;
            }

            /**
             * Creates a <code>Motion</code> object with given values for position and velocity.
             * @param position the initial position value of this motion, given in [m] or [rad].
             * @param velocity the initial velocity value of this motion, given in [m/s] or [rad/s].
             */
            public Motion(double position, double velocity)
            {

                this.position = position;
                this.velocity = velocity;

                profileVelocity = GlobalMembers.DEFAULT_LIMIT;
                profileAcceleration = GlobalMembers.DEFAULT_LIMIT;
                profileDeceleration = GlobalMembers.DEFAULT_LIMIT;
            }

            /**
             * Creates a <code>Motion</code> object with given values for position and velocity.
             * @param motion another <code>Motion</code> object to copy the values from.
             */
            public Motion(Motion motion)
            {

                position = motion.position;
                velocity = motion.velocity;

                profileVelocity = motion.profileVelocity;
                profileAcceleration = motion.profileAcceleration;
                profileDeceleration = motion.profileDeceleration;
            }
            public virtual void Dispose()
            {
            }

            /**
             * Sets the position value.
             * @param position the desired position value of this motion, given in [m] or [rad].
             */
            public void setPosition(double position)
            {

                this.position = position;
            }

            /**
             * Gets the position value.
             * @return the position value of this motion, given in [m] or [rad].
             */
            public double getPosition()
            {

                return position;
            }

            /**
             * Sets the velocity value.
             * @param velocity the desired velocity value of this motion, given in [m/s] or [rad/s].
             */
            public void setVelocity(double velocity)
            {

                this.velocity = velocity;
            }

            /**
             * Gets the velocity value.
             * @return the velocity value of this motion, given in [m/s] or [rad/s].
             */
            public double getVelocity()
            {

                return velocity;
            }

            /**
             * Sets the limit for the velocity value.
             * @param profileVelocity the limit of the velocity.
             */
            public void setProfileVelocity(double profileVelocity)
            {

                if (profileVelocity > GlobalMembers.MINIMUM_LIMIT)
                {
                    this.profileVelocity = profileVelocity;
                }
                else
                {
                    this.profileVelocity = GlobalMembers.MINIMUM_LIMIT;
                }
            }

            /**
             * Sets the limit for the acceleration value.
             * @param profileAcceleration the limit of the acceleration.
             */
            public void setProfileAcceleration(double profileAcceleration)
            {

                if (profileAcceleration > GlobalMembers.MINIMUM_LIMIT)
                {
                    this.profileAcceleration = profileAcceleration;
                }
                else
                {
                    this.profileAcceleration = GlobalMembers.MINIMUM_LIMIT;
                }
            }

            /**
             * Sets the limit for the deceleration value.
             * @param profileDeceleration the limit of the deceleration.
             */
            public void setProfileDeceleration(double profileDeceleration)
            {

                if (profileDeceleration > GlobalMembers.MINIMUM_LIMIT)
                {
                    this.profileDeceleration = profileDeceleration;
                }
                else
                {
                    this.profileDeceleration = GlobalMembers.MINIMUM_LIMIT;
                }
            }

            /**
             * Sets the limits for velocity, acceleration and deceleration values.
             * @param profileVelocity the limit of the velocity.
             * @param profileAcceleration the limit of the acceleration.
             * @param profileDeceleration the limit of the deceleration.
             */
            public void setLimits(double profileVelocity, double profileAcceleration, double profileDeceleration)
            {
                if (profileAcceleration < 0.001 || profileDeceleration < 0.001)
                    Console.WriteLine("fehler");

                if (profileVelocity > GlobalMembers.MINIMUM_LIMIT)
                {
                    this.profileVelocity = profileVelocity;
                }
                else
                {
                    this.profileVelocity = GlobalMembers.MINIMUM_LIMIT;
                }
                if (profileAcceleration > GlobalMembers.MINIMUM_LIMIT)
                {
                    this.profileAcceleration = profileAcceleration;
                }
                else
                {
                    this.profileAcceleration = GlobalMembers.MINIMUM_LIMIT;
                }
                if (profileDeceleration > GlobalMembers.MINIMUM_LIMIT)
                {
                    this.profileDeceleration = profileDeceleration;
                }
                else
                {
                    this.profileDeceleration = GlobalMembers.MINIMUM_LIMIT;
                }
            }

            double tacc = 0.0;
            double tdecc = 0.0;

            public double getTimeForAcceleration()
            {
                return tacc;// profileVelocity / profileAcceleration;       
            }
            public double getTimeForDecceleration()
            {
                return tdecc;// profileVelocity / profileDeceleration;
            }

            /**
             * Gets the time needed to move to a given target position.
             * @param targetPosition the desired target position given in [m] or [rad].
             * @return the time to move to the target position, given in [s].
             */
            public double getTimeToPosition(double targetPosition)
            {

                // calculate position, when velocity is reduced to zero

                double stopPosition = (velocity > 0.0f) ? position + (double)(velocity * velocity / profileDeceleration * 0.5f) : position - (double)(velocity * velocity / profileDeceleration * 0.5f);

                if (targetPosition > stopPosition)
                { // positive velocity required

                    if (velocity > profileVelocity)
                    { // slow down to profile velocity first

                        double t1 = (velocity - profileVelocity) / profileDeceleration;
                        double t2 = (double)(targetPosition - stopPosition) / profileVelocity;
                        double t3 = profileVelocity / profileDeceleration;

                        tacc = t2;
                        tdecc = t3;

                        return t1 + t2 + t3;

                    }
                    else if (velocity > 0.0f)
                    { // speed up to profile velocity

                        double t1 = (profileVelocity - velocity) / profileAcceleration;
                        double t3 = profileVelocity / profileDeceleration;
                        double t2 = ((double)(targetPosition - position) - (velocity + profileVelocity) * 0.5f * t1) / profileVelocity - 0.5f * t3;

                        tacc = t2;
                        tdecc = t3;

                        if (t2 < 0.0f)
                        {
                            double maxVelocity = Math.Sqrt((2.0f * (double)(targetPosition - position) * profileAcceleration + velocity * velocity) * profileDeceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(maxVelocity)) maxVelocity = 0;
                            t1 = (maxVelocity - velocity) / profileAcceleration;
                            t2 = 0.0f;
                            t3 = maxVelocity / profileDeceleration;

                            tacc = t1;
                            tdecc = t3;
                        }

                        return t1 + t2 + t3;

                    }
                    else
                    { // slow down to zero first, and then speed up to profile velocity

                        double t1 = -velocity / profileDeceleration;
                        double t2 = profileVelocity / profileAcceleration;
                        double t4 = profileVelocity / profileDeceleration;
                        double t3 = ((double)(targetPosition - position) - velocity * 0.5f * t1) / profileVelocity - 0.5f * (t2 + t4);

                        tacc = t2;
                        tdecc = t4;

                        if (t3 < 0.0f)
                        {
                            double maxVelocity = Math.Sqrt((2.0f * (double)(targetPosition - position) * profileDeceleration + velocity * velocity) * profileAcceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(maxVelocity)) maxVelocity = 0;
                            t2 = maxVelocity / profileAcceleration;
                            t3 = 0.0f;
                            t4 = maxVelocity / profileDeceleration;

                            tacc = t2;
                            tdecc = t4;
                        }

                        return t1 + t2 + t3 + t4;
                    }

                }
                else
                { // negative velocity required

                    if (velocity < -profileVelocity)
                    { // slow down to (negative) profile velocity first

                        double t1 = (-profileVelocity - velocity) / profileDeceleration;
                        double t2 = (double)(stopPosition - targetPosition) / profileVelocity;
                        double t3 = profileVelocity / profileDeceleration;

                        tacc = t1;
                        tdecc = t3;

                        return t1 + t2 + t3;

                    }
                    else if (velocity < 0.0f)
                    { // speed up to (negative) profile velocity

                        double t1 = (velocity + profileVelocity) / profileAcceleration;
                        double t3 = profileVelocity / profileDeceleration;
                        double t2 = ((double)(position - targetPosition) + (velocity - profileVelocity) * 0.5f * t1) / profileVelocity - 0.5f * t3;

                        if (t2 < 0.0f)
                        {
                            double minVelocity = -Math.Sqrt((-2.0f * (double)(targetPosition - position) * profileAcceleration + velocity * velocity) * profileDeceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(minVelocity)) minVelocity = 0;
                            t1 = (velocity - minVelocity) / profileAcceleration;
                            t2 = 0.0f;
                            t3 = -minVelocity / profileDeceleration;
                        }

                        tacc = t1;
                        tdecc = t3;

                        return t1 + t2 + t3;

                    }
                    else
                    { // slow down to zero first, and then speed up to (negative) profile velocity

                        double t1 = velocity / profileDeceleration;
                        double t2 = profileVelocity / profileAcceleration;
                        double t4 = profileVelocity / profileDeceleration;
                        double t3 = (-(double)(targetPosition - position) + velocity * 0.5f * t1) / profileVelocity - 0.5f * (t2 + t4);

                        if (t3 < 0.0f)
                        {
                            double minVelocity = -Math.Sqrt((-2.0f * (double)(targetPosition - position) * profileDeceleration + velocity * velocity) * profileAcceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(minVelocity)) minVelocity = 0;
                            t2 = -minVelocity / profileAcceleration;
                            t3 = 0.0f;
                            t4 = -minVelocity / profileDeceleration;
                        }

                        tacc = t2;
                        tdecc = t4;

                        return t1 + t2 + t3 + t4;
                    }
                }
            }

            /**
             * Increments the current motion towards a given target velocity.
             * @param targetVelocity the desired target velocity given in [m/s] or [rad/s].
             * @param period the time period to increment the motion values for, given in [s].
             */
            public void incrementToVelocity(double targetVelocity, double period)
            {

                if (targetVelocity < -profileVelocity)
                {
                    targetVelocity = -profileVelocity;
                }
                else if (targetVelocity > profileVelocity)
                {
                    targetVelocity = profileVelocity;
                }

                if (targetVelocity > 0.0f)
                {

                    if (velocity > targetVelocity)
                    { // slow down to target velocity

                        double t1 = (velocity - targetVelocity) / profileDeceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * period) * period);
                            velocity += -profileDeceleration * period;
                        }
                        else
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }

                    }
                    else if (velocity > 0.0f)
                    { // speed up to target velocity

                        double t1 = (targetVelocity - velocity) / profileAcceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity + profileAcceleration * 0.5f * period) * period);
                            velocity += profileAcceleration * period;
                        }
                        else
                        {
                            position += (double)((velocity + profileAcceleration * 0.5f * t1) * t1);
                            velocity += profileAcceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }

                    }
                    else
                    { // slow down to zero first, and then speed up to target velocity

                        double t1 = -velocity / profileDeceleration;
                        double t2 = targetVelocity / profileAcceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * period) * period);
                            velocity += profileDeceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)((velocity + profileAcceleration * 0.5f * (period - t1)) * (period - t1));
                            velocity += profileAcceleration * (period - t1);
                        }
                        else
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)((velocity + profileAcceleration * 0.5f * t2) * t2);
                            velocity += profileAcceleration * t2;
                            position += (double)(velocity * (period - t1 - t2));
                        }
                    }

                }
                else
                {

                    if (velocity < targetVelocity)
                    { // slow down to (negative) target velocity

                        double t1 = (targetVelocity - velocity) / profileDeceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * period) * period);
                            velocity += profileDeceleration * period;
                        }
                        else
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }

                    }
                    else if (velocity < 0.0f)
                    { // speed up to (negative) target velocity

                        double t1 = (velocity - targetVelocity) / profileAcceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity - profileAcceleration * 0.5f * period) * period);
                            velocity += -profileAcceleration * period;
                        }
                        else
                        {
                            position += (double)((velocity - profileAcceleration * 0.5f * t1) * t1);
                            velocity += -profileAcceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }

                    }
                    else
                    { // slow down to zero first, and then speed up to (negative) target velocity

                        double t1 = velocity / profileDeceleration;
                        double t2 = -targetVelocity / profileAcceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * period) * period);
                            velocity += -profileDeceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)((velocity - profileAcceleration * 0.5f * (period - t1)) * (period - t1));
                            velocity += -profileAcceleration * (period - t1);
                        }
                        else
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)((velocity - profileAcceleration * 0.5f * t2) * t2);
                            velocity += -profileAcceleration * t2;
                            position += (double)(velocity * (period - t1 - t2));
                        }
                    }
                }
            }

            /**
             * Increments the current motion towards a given target position.
             * @param targetPosition the desired target position given in [m] or [rad].
             * @param period the time period to increment the motion values for, given in [s].
             */
            public void incrementToPosition(double targetPosition, double period)
            {

                // calculate position, when velocity is reduced to zero

                double stopPosition = (velocity > 0.0f) ? position + (double)(velocity * velocity / profileDeceleration * 0.5f) : position - (double)(velocity * velocity / profileDeceleration * 0.5f);

                if (targetPosition > stopPosition)
                { // positive velocity required

                    if (velocity > profileVelocity)
                    { // slow down to profile velocity first

                        double t1 = (velocity - profileVelocity) / profileDeceleration;
                        double t2 = (double)(targetPosition - stopPosition) / profileVelocity;
                        double t3 = profileVelocity / profileDeceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * period) * period);
                            velocity += -profileDeceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }
                        else if (t1 + t2 + t3 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity - profileDeceleration * 0.5f * (period - t1 - t2)) * (period - t1 - t2));
                            velocity += -profileDeceleration * (period - t1 - t2);
                        }
                        else
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity - profileDeceleration * 0.5f * t3) * t3);
                            velocity += -profileDeceleration * t3;
                        }

                    }
                    else if (velocity > 0.0f)
                    { // speed up to profile velocity

                        double t1 = (profileVelocity - velocity) / profileAcceleration;
                        double t3 = (profileVelocity)/ profileDeceleration;
                        double t2 = ((double)(targetPosition - position) - (velocity + profileVelocity) * 0.5f * t1) / profileVelocity - 0.5f * t3;

                        if (t2 < 0.0f)
                        {
                            double maxVelocity = Math.Sqrt((2.0f * (double)(targetPosition - position) * profileAcceleration + velocity * velocity) * profileDeceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(maxVelocity)) maxVelocity = 0;
                            t1 = (maxVelocity - velocity) / profileAcceleration;
                            t2 = 0.0f;
                            t3 = maxVelocity / profileDeceleration;
                        }

                        if (t1 > period)
                        {
                            position += (double)((velocity + profileAcceleration * 0.5f * period) * period);
                            velocity += profileAcceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity + profileAcceleration * 0.5f * t1) * t1);
                            velocity += profileAcceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }
                        else if (t1 + t2 + t3 > period)
                        {
                            position += (double)((velocity + profileAcceleration * 0.5f * t1) * t1);
                            velocity += profileAcceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity - profileDeceleration * 0.5f * (period - t1 - t2)) * (period - t1 - t2));
                            velocity += -profileDeceleration * (period - t1 - t2);
                        }
                        else
                        {
                            position += (double)((velocity + profileAcceleration * 0.5f * t1) * t1);
                            velocity += profileAcceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity - profileDeceleration * 0.5f * t3) * t3);
                            velocity += -profileDeceleration * t3;
                        }

                    }
                    else
                    { // slow down to zero first, and then speed up to profile velocity

                        double t1 = -velocity / profileDeceleration;
                        double t2 = profileVelocity / profileAcceleration;
                        double t4 = profileVelocity / profileDeceleration;
                        double t3 = ((double)(targetPosition - position) - velocity * 0.5f * t1) / profileVelocity - 0.5f * (t2 + t4);

                        if (t3 < 0.0f)
                        {
                            double maxVelocity = Math.Sqrt((2.0f * (double)(targetPosition - position) * profileDeceleration + velocity * velocity) * profileAcceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(maxVelocity)) maxVelocity = 0;
                            t2 = maxVelocity / profileAcceleration;
                            t3 = 0.0f;
                            t4 = maxVelocity / profileDeceleration;
                        }

                        if (t1 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * period) * period);
                            velocity += profileDeceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)((velocity + profileAcceleration * 0.5f * (period - t1)) * (period - t1));
                            velocity += profileAcceleration * (period - t1);
                        }
                        else if (t1 + t2 + t3 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)((velocity + profileAcceleration * 0.5f * t2) * t2);
                            velocity += profileAcceleration * t2;
                            position += (double)(velocity * (period - t1 - t2));
                        }
                        else if (t1 + t2 + t3 + t4 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)((velocity + profileAcceleration * 0.5f * t2) * t2);
                            velocity += profileAcceleration * t2;
                            position += (double)(velocity * t3);
                            position += (double)((velocity - profileDeceleration * 0.5f * (period - t1 - t2 - t3)) * (period - t1 - t2 - t3));
                            velocity += -profileDeceleration * (period - t1 - t2 - t3);
                        }
                        else
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)((velocity + profileAcceleration * 0.5f * t2) * t2);
                            velocity += profileAcceleration * t2;
                            position += (double)(velocity * t3);
                            position += (double)((velocity - profileDeceleration * 0.5f * t4) * t4);
                            velocity += -profileDeceleration * t4;
                        }
                    }

                }
                else
                { // negative velocity required

                    if (velocity < -profileVelocity)
                    { // slow down to (negative) profile velocity first

                        double t1 = (-profileVelocity - velocity) / profileDeceleration;
                        double t2 = (double)(stopPosition - targetPosition) / profileVelocity;
                        double t3 = profileVelocity / profileDeceleration;

                        if (t1 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * period) * period);
                            velocity += profileDeceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }
                        else if (t1 + t2 + t3 > period)
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity + profileDeceleration * 0.5f * (period - t1 - t2)) * (period - t1 - t2));
                            velocity += profileDeceleration * (period - t1 - t2);
                        }
                        else
                        {
                            position += (double)((velocity + profileDeceleration * 0.5f * t1) * t1);
                            velocity += profileDeceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity + profileDeceleration * 0.5f * t3) * t3);
                            velocity += profileDeceleration * t3;
                        }

                    }
                    else if (velocity < 0.0f)
                    { // speed up to (negative) profile velocity

                        double t1 = (velocity + profileVelocity) / profileAcceleration;
                        double t3 = profileVelocity / profileDeceleration;
                        double t2 = ((double)(position - targetPosition) + (velocity - profileVelocity) * 0.5f * t1) / profileVelocity - 0.5f * t3;

                        if (t2 < 0.0f)
                        {
                            double minVelocity = -Math.Sqrt((-2.0f * (double)(targetPosition - position) * profileAcceleration + velocity * velocity) * profileDeceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(minVelocity)) minVelocity = 0;
                            t1 = (velocity - minVelocity) / profileAcceleration;
                            t2 = 0.0f;
                            t3 = -minVelocity / profileDeceleration;
                        }

                        if (t1 > period)
                        {
                            position += (double)((velocity - profileAcceleration * 0.5f * period) * period);
                            velocity += -profileAcceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity - profileAcceleration * 0.5f * t1) * t1);
                            velocity += -profileAcceleration * t1;
                            position += (double)(velocity * (period - t1));
                        }
                        else if (t1 + t2 + t3 > period)
                        {
                            position += (double)((velocity - profileAcceleration * 0.5f * t1) * t1);
                            velocity += -profileAcceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity + profileDeceleration * 0.5f * (period - t1 - t2)) * (period - t1 - t2));
                            velocity += profileDeceleration * (period - t1 - t2);
                        }
                        else
                        {
                            position += (double)((velocity - profileAcceleration * 0.5f * t1) * t1);
                            velocity += -profileAcceleration * t1;
                            position += (double)(velocity * t2);
                            position += (double)((velocity + profileDeceleration * 0.5f * t3) * t3);
                            velocity += profileDeceleration * t3;
                        }

                    }
                    else
                    { // slow down to zero first, and then speed up to (negative) profile velocity

                        double t1 = velocity / profileDeceleration;
                        double t2 = profileVelocity / profileAcceleration;
                        double t4 = profileVelocity / profileDeceleration;
                        double t3 = (-(double)(targetPosition - position) + velocity * 0.5f * t1) / profileVelocity - 0.5f * (t2 + t4);

                        if (t3 < 0.0f)
                        {
                            double minVelocity = -Math.Sqrt((-2.0f * (double)(targetPosition - position) * profileDeceleration + velocity * velocity) * profileAcceleration / (profileAcceleration + profileDeceleration));
                            if (double.IsNaN(minVelocity)) minVelocity = 0;
                            t2 = -minVelocity / profileAcceleration;
                            t3 = 0.0f;
                            t4 = -minVelocity / profileDeceleration;
                        }

                        if (t1 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * period) * period);
                            velocity += -profileDeceleration * period;
                        }
                        else if (t1 + t2 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)((velocity - profileAcceleration * 0.5f * (period - t1)) * (period - t1));
                            velocity += -profileAcceleration * (period - t1);
                        }
                        else if (t1 + t2 + t3 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)((velocity - profileAcceleration * 0.5f * t2) * t2);
                            velocity += -profileAcceleration * t2;
                            position += (double)(velocity * (period - t1 - t2));
                        }
                        else if (t1 + t2 + t3 + t4 > period)
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)((velocity - profileAcceleration * 0.5f * t2) * t2);
                            velocity += -profileAcceleration * t2;
                            position += (double)(velocity * t3);
                            position += (double)((velocity + profileDeceleration * 0.5f * (period - t1 - t2 - t3)) * (period - t1 - t2 - t3));
                            velocity += profileDeceleration * (period - t1 - t2 - t3);
                        }
                        else
                        {
                            position += (double)((velocity - profileDeceleration * 0.5f * t1) * t1);
                            velocity += -profileDeceleration * t1;
                            position += (double)((velocity - profileAcceleration * 0.5f * t2) * t2);
                            velocity += -profileAcceleration * t2;
                            position += (double)(velocity * t3);
                            position += (double)((velocity + profileDeceleration * 0.5f * t4) * t4);
                            velocity += profileDeceleration * t4;
                        }
                    }
                }
            }


            private readonly double DEFAULT_LIMIT; // default value for limits
            private readonly double MINIMUM_LIMIT; // smallest value allowed for limits

            private double profileVelocity;
            private double profileAcceleration;
            private double profileDeceleration;
        }

        /**
         * Sets the values for position and velocity.
         * @param position the desired position value of this motion, given in [m] or [rad].
         * @param velocity the desired velocity value of this motion, given in [m/s] or [rad/s].
         */

        /**
         * Sets the values for position and velocity.
         * @param motion another <code>Motion</code> object to copy the values from.
         */
    }
}