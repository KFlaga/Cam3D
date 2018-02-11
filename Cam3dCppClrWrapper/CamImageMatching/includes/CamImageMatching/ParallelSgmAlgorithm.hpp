#pragma once

#include "SgmCommon.hpp"
#include "SgmCostAggregator.hpp"
#include "CensusCostComputer.hpp"
#include <CamCommon\TaskQueue.hpp>

namespace cam3d
{
	template<typename SgmAggregator>
	class ParallelSgmAlgorithm : public ISgmCostAggregator
	{
		using Image = typename SgmAggregator::Image;
		StaticTaskQueue queue;
		DisparityMap& mapLeft;
		DisparityMap& mapRight;
		Image& imageLeft;
		Image& imageRight;
		SgmParameters params;

		SgmAggregator sgmLeft;
		SgmAggregator sgmRight;

	public:
		ParallelSgmAlgorithm(SgmParameters& params, DisparityMap& mapLeft, DisparityMap& mapRight,
			Image& imageLeft, Image& imageRight) :
				queue{ params.maxParallelTasks }, mapLeft{mapLeft}, mapRight{mapRight},
				imageLeft{imageLeft}, imageRight{imageRight}, params{params},
				sgmLeft{ params, true, imageLeft, imageRight, mapLeft },
				sgmRight{ params, false, imageRight, imageLeft, mapRight }
		{ }

		void computeMatchingCosts() override
		{
			enum Tasks
			{
				leftCensus,
				leftPaths,
				leftTopDown,
				leftBottomUp,
				leftDisp,
				rightCensus,
				rightPaths,
				rightTopDown,
				rightBottomUp,
				rightDisp,
				leftAll,
				rightAll
			};

			queue.addTask(Task{ leftCensus, [this]() {
				sgmLeft.initLocalCosts();
			} }, {});
			queue.addTask(Task{ leftPaths, [this]() {
				sgmLeft.initPaths();
			} }, { leftCensus });
			queue.addTask(Task{ leftTopDown, [this]() {
				sgmLeft.findCostsTopDown();
			} }, { leftPaths });
			queue.addTask(Task{ leftBottomUp, [this]() {
				sgmLeft.findCostsBottomUp();
			} }, { leftPaths });
			queue.addTask(Task{ leftDisp, [this]() {
				sgmLeft.findDisparities();
				sgmLeft.done();
			} }, { leftTopDown, leftBottomUp });

			queue.addTask(Task{ rightCensus, [this]() {
				sgmRight.initLocalCosts();
			} }, {});
			queue.addTask(Task{ rightPaths, [this]() {
				sgmRight.initPaths();
			} }, { rightCensus });
			queue.addTask(Task{ rightTopDown, [this]() {
				sgmRight.findCostsTopDown();
			} }, { rightPaths });
			queue.addTask(Task{ rightBottomUp, [this]() {
				sgmRight.findCostsBottomUp();
			} }, { rightPaths });
			queue.addTask(Task{ rightDisp, [this]() {
				sgmRight.findDisparities();
				sgmRight.done();
			} }, { rightTopDown, rightBottomUp });

			/*queue.addTask(Task{ leftAll, [this]() {
				sgmLeft.computeMatchingCosts();
			} }, {});
			queue.addTask(Task{ rightAll, [this]() {
				sgmRight.computeMatchingCosts();
			} }, {});*/

			queue.run();
		}

		void terminate() override
		{
			sgmLeft.terminate();
			sgmRight.terminate();
		}

		std::string getState() override
		{
			return "LEFT: " + sgmLeft.getState() + ", RIGHT: " + sgmRight.getState();
		}
	};
}