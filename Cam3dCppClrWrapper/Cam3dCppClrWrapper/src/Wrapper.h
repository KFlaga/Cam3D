#pragma once

namespace Cam3dWrapper
{
	public ref class IWrapper abstract
	{
	public:
		virtual void* getNative() = 0;

		template<typename Ty>
		Ty* getNativeAs()
		{
			return reinterpret_cast<Ty*>(getNative());
		}

		virtual void Update() = 0;
	
	internal:
		virtual void updateNative() = 0;
	};

	template<typename Ty>
	public ref class Wrapper abstract : public IWrapper
	{
	public:
		virtual void* getNative() override
		{
			return reinterpret_cast<void*>(native);
		}

	protected:
		Ty* native;
	};
}