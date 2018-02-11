#pragma once

#include <chrono>
#include <string>
#include <iostream>

namespace cam3d
{
    namespace profiling
    {
        typedef unsigned int Microseconds;
        class OneShotClock
        {
        private:
            std::chrono::time_point<std::chrono::high_resolution_clock> onStart;
            std::chrono::time_point<std::chrono::high_resolution_clock> onFinish;
            std::chrono::microseconds elapsedTime;

        public:
            OneShotClock() = default;

            void start()
            {
                onStart = std::chrono::high_resolution_clock::now();
            }

            void finish()
            {
                onFinish = std::chrono::high_resolution_clock::now();
                elapsedTime = std::chrono::duration_cast<std::chrono::microseconds>(onFinish - onStart);
            }

            Microseconds getElapsedMs() const
            {
                return elapsedTime.count();
            }
        };

        class ProfileScope
        {
        private:
            OneShotClock clock;
            Microseconds* elapsedResult;

        public:
            ProfileScope(Microseconds* placeToSaveResult) :
                clock{},
                elapsedResult{placeToSaveResult}
            {
                clock.start();
            }

            ~ProfileScope()
            {
                clock.finish();
                *elapsedResult = clock.getElapsedMs();
            }
        };
    }
}