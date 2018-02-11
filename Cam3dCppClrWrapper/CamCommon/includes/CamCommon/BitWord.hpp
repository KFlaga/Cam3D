#pragma once

#include "PreReqs.hpp"
#include <algorithm>
#include <cstring>

namespace cam3d
{
	template<typename uint_t>
	struct WordBitsLut
	{
	private:
		static constexpr size_t wordBitsSize = ((size_t)std::numeric_limits<uint16_t>::max()) + 1;

	public:
		static uint_t* createWordBitsLut()
		{
			static uint_t wordBits[wordBitsSize];
			uint_t count;
			for (size_t i = 0, x; i < wordBitsSize; ++i)
			{
				x = i;
				for (count = 0u; x > 0; ++count) { x &= x - 1; }
				wordBits[i] = count;
			}
			return wordBits;
		}
	};

	struct HammingLookup32
	{
		static inline uint32_t getOnesCount(uint32_t i)
		{
			static uint32_t* wordBits = WordBitsLut<uint32_t>::createWordBitsLut();
			return (wordBits[i & 0x0000FFFF] + wordBits[i >> 16]);
		}
	};

	struct HammingLookup64
	{
		static inline uint64_t getOnesCount(uint64_t i)
		{
			static uint64_t* wordBits = WordBitsLut<uint64_t>::createWordBitsLut();
			return (wordBits[i & 0xFFFF] + wordBits[(i >> 16) & 0xFFFF] + wordBits[(i >> 32) & 0xFFFF] + wordBits[(i >> 48) & 0xFFFF]);
		}
	};

	template<typename BWT>
	struct BitWord_helper
	{
	};

	template<size_t length, size_t bits, typename uint_type, typename HammingLookupT>
	struct BitWord
	{
		static constexpr size_t lengthInWords = length;
		static constexpr size_t bitSizeOfWord = bits;
		using HammingLookup = HammingLookupT;
		using uint_t = uint_type;

		uint_t bytes[length];
		BitWord()
		{
			std::memset(bytes, 0, sizeof(bytes));
		}

		BitWord(uint_t* bytes_)
		{
			std::memcpy(bytes, bytes_, sizeof(bytes));
		}

	public:
		inline uint_t getHammingDistance(const BitWord& bw) const
		{
			return BitWord_helper<BitWord>::getOnesCount(bytes, bw.bytes);
		}
	};

	template<size_t length>
	using BitWord32 = BitWord<length, 32, uint32_t, HammingLookup32>;
	template<size_t length>
	using BitWord64 = BitWord<length, 64, uint64_t, HammingLookup64>;

	// CLI non-working template recursion workaround

	template<>
	struct BitWord_helper<BitWord32<1>>
	{
		using BW = BitWord32<1>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<2>>
	{
		using BW = BitWord32<2>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<3>>
	{
		using BW = BitWord32<3>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<4>>
	{
		using BW = BitWord32<4>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]) +
				BW::HammingLookup::getOnesCount(b1[3] ^ b2[3]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<5>>
	{
		using BW = BitWord32<5>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]) +
				BW::HammingLookup::getOnesCount(b1[3] ^ b2[3]) +
				BW::HammingLookup::getOnesCount(b1[4] ^ b2[4]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<6>>
	{
		using BW = BitWord32<6>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]) +
				BW::HammingLookup::getOnesCount(b1[3] ^ b2[3]) +
				BW::HammingLookup::getOnesCount(b1[4] ^ b2[4]) +
				BW::HammingLookup::getOnesCount(b1[5] ^ b2[5]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<7>>
	{
		using BW = BitWord32<7>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]) +
				BW::HammingLookup::getOnesCount(b1[3] ^ b2[3]) +
				BW::HammingLookup::getOnesCount(b1[4] ^ b2[4]) +
				BW::HammingLookup::getOnesCount(b1[5] ^ b2[5]) +
				BW::HammingLookup::getOnesCount(b1[6] ^ b2[6]);
		}
	};

	template<>
	struct BitWord_helper<BitWord32<8>>
	{
		using BW = BitWord32<8>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]) +
				BW::HammingLookup::getOnesCount(b1[3] ^ b2[3]) +
				BW::HammingLookup::getOnesCount(b1[4] ^ b2[4]) +
				BW::HammingLookup::getOnesCount(b1[5] ^ b2[5]) +
				BW::HammingLookup::getOnesCount(b1[4] ^ b2[4]) +
				BW::HammingLookup::getOnesCount(b1[5] ^ b2[5]);
		}
	};

	template<>
	struct BitWord_helper<BitWord64<1>>
	{
		using BW = BitWord64<1>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]);
		}
	};

	template<>
	struct BitWord_helper<BitWord64<2>>
	{
		using BW = BitWord64<2>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]);
		}
	};

	template<>
	struct BitWord_helper<BitWord64<3>>
	{
		using BW = BitWord64<3>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]);
		}
	};

	template<>
	struct BitWord_helper<BitWord64<4>>
	{
		using BW = BitWord64<4>;

		static BW::uint_t getOnesCount(const BW::uint_t* b1, const BW::uint_t* b2)
		{
			return BW::HammingLookup::getOnesCount(b1[0] ^ b2[0]) +
				BW::HammingLookup::getOnesCount(b1[1] ^ b2[1]) +
				BW::HammingLookup::getOnesCount(b1[2] ^ b2[2]) +
				BW::HammingLookup::getOnesCount(b1[3] ^ b2[3]);
		}
	};
}
