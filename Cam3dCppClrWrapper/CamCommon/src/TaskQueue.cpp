#include "TaskQueue.hpp"

namespace cam3d
{
StaticTaskQueue::StaticTaskQueue(std::size_t maxRunningTasks) :
    maxRunningTasks{maxRunningTasks},
    nextTaskIdx{0},
    shouldEnd{false},
	running{false}
{ }

StaticTaskQueue::~StaticTaskQueue()
{
    end();
	while (isRunning()) { std::this_thread::yield(); } // ??
}

void StaticTaskQueue::addTask(Task task, const std::vector<TaskId>& dependencies)
{
    idToIndex[task.id] = allTasks.size();
    allTasks.push_back({task, allTasks.size(), dependencies, (int)dependencies.size()});
}

bool StaticTaskQueue::isAllReadyQueued() const
{
    return readyTasks.size() == nextTaskIdx;
}

bool StaticTaskQueue::isAllDone() const
{
	return nextTaskIdx == allTasks.size() && runningTasks.empty();
}

void StaticTaskQueue::end()
{
    shouldEnd = true;
}

void StaticTaskQueue::run()
{
	running = true;
    prepare();
    while(!(shouldEnd || isAllDone()))
    {
        runAvailableTasks();
        updateFinishedTasks();
    }
	running = false;
}

std::pair<StaticTaskQueue::TaskIndex, Task> StaticTaskQueue::getNextTask()
{
    nextTaskIdx++;
    return {nextTaskIdx-1, allTasks[readyTasks[nextTaskIdx-1]].task};
}

void StaticTaskQueue::runAvailableTasks()
{
    while(runningTasks.size() < maxRunningTasks && !isAllReadyQueued())
    {
        std::pair<TaskIndex, Task> nextTask = getNextTask();
        std::future<TaskIndex> result = std::async(std::launch::async, [nextTask]()
        {
            nextTask.second.task();
            return nextTask.first;
        });
        runningTasks.push_back(std::move(result));
    }
}

void StaticTaskQueue::updateFinishedTasks()
{
    std::vector<std::future<TaskIndex>> remainingTasks{};
    for(std::size_t i = 0; i < runningTasks.size(); ++i)
    {
        if(runningTasks[i].wait_for(std::chrono::milliseconds(1)) == std::future_status::ready)
        {
            int doneIdx = runningTasks[i].get();
            updateGraph(doneIdx);
        }
        else
        {
            remainingTasks.push_back(std::move(runningTasks[i]));
        }
    }
    runningTasks = std::move(remainingTasks);
}

void StaticTaskQueue::prepare()
{
    dependencyGraph.resize(allTasks.size());
    for(TaskIndex idx = 0; idx < allTasks.size(); ++idx)
    {
        if(allTasks[idx].dependencies.size() == 0)
        {
            readyTasks.push_back(idx);
        }
        else
        {
            for(TaskId dId: allTasks[idx].dependencies)
            {
                dependencyGraph[idToIndex[dId]].push_back(idx);
            }
        }
    }
}

void StaticTaskQueue::updateGraph(int doneIdx)
{
    DependentOnList& dependentsOnDone = dependencyGraph[readyTasks[doneIdx]];
    for(TaskIndex idx: dependentsOnDone)
    {
        allTasks[idx].dependencyCounter--;
        if(allTasks[idx].dependencyCounter == 0)
        {
            readyTasks.push_back(idx);
        }
    }
}

}
