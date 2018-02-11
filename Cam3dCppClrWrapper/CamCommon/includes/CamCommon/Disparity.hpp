#pragma once

#include <CamCommon/Vector2.hpp>

namespace cam3d
{
struct Disparity
{
    enum DisparityFlags : int
    {
        Invalid = 0,
        Valid = 1,
        Occluded = 2
    };

    int dx;
    DisparityFlags flags;
    double subDx;
    double cost;
    double confidence;

    Disparity() { }

    Disparity(int dx_, DisparityFlags flags_ = Invalid) :
        dx{dx_},
        flags{flags_},
        subDx{static_cast<double>(dx_)},
        cost{1e12},
        confidence{0.0}
    { }

    Disparity(int dx_, DisparityFlags flags_, double subDx_, double cost_, double confidence_) :
        dx{dx_},
        flags{flags_},
        subDx{subDx_},
        cost{cost_},
        confidence{confidence_}
    { }

    Disparity(Point2 base, Point2 matched, DisparityFlags flags_, double cost_, double confidence_) :
        dx{matched.x - base.x},
        flags{flags_},
        subDx{ static_cast<double>(dx) },
        cost{cost_},
        confidence{confidence_}
    { }
};
}
