using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;

namespace SonicRetro.SonLVL.API
{
	/// <summary>
	/// Represents a ring layout format.
	/// </summary>
	public abstract class RingFormat
	{
		public abstract Entry CreateRing();
	}

	public abstract class RingLayoutFormat : RingFormat
	{
		/// <summary>
		/// The default compression used for layout files.
		/// </summary>
		public virtual CompressionType DefaultCompression { get { return CompressionType.Uncompressed; } }

		public abstract List<RingEntry> ReadLayout(byte[] rawdata);

		public List<RingEntry> ReadLayout(string filename, CompressionType compression)
		{
			if (compression == CompressionType.Invalid) compression = DefaultCompression;
			LevelData.Log("Loading rings from file \"" + filename + "\", using compression " + compression + "...");
			return ReadLayout(Compression.Decompress(filename, compression));
		}

		public List<RingEntry> ReadLayout(string filename) { return ReadLayout(filename, DefaultCompression); }

		public List<RingEntry> TryReadLayout(string filename, CompressionType compression)
		{
			if (File.Exists(filename))
				return ReadLayout(filename, compression);
			else
			{
				LevelData.Log("Ring file \"" + filename + "\" not found.");
				return new List<RingEntry>();
			}
		}

		public List<RingEntry> TryReadLayout(string filename) { return TryReadLayout(filename, DefaultCompression); }

		public abstract byte[] WriteLayout(List<RingEntry> rings);

		public void WriteLayout(List<RingEntry> rings, CompressionType compression, string filename)
		{
			if (compression == CompressionType.Invalid) compression = DefaultCompression;
			Compression.Compress(WriteLayout(rings), filename, compression);
		}

		public void WriteLayout(List<RingEntry> rings, string filename) { WriteLayout(rings, DefaultCompression, filename); }

		public abstract void Init(ObjectData data);

		public abstract string Name { get; }

		public abstract Sprite Image { get; }

		public abstract Sprite GetSprite(RingEntry rng);

		public abstract Rectangle GetBounds(RingEntry rng, Point camera);

		public abstract int CountRings(IEnumerable<RingEntry> rings);
	}

	public abstract class RingObjectFormat : RingFormat
	{
		public abstract byte ObjectID { get; }

		public override Entry CreateRing()
		{
			return LevelData.CreateObject(ObjectID);
		}

		public int CountRings(IEnumerable<ObjectEntry> objects)
		{
			PropertySpec prop = LevelData.GetObjectDefinition(ObjectID).CustomProperties.SingleOrDefault(a => a.Name == "Count");
			if (prop != null)
				return objects.Where(a => a.ID == ObjectID).Sum(a => (int)prop.GetValue(a));
			else
				return objects.Count(a => a.ID == ObjectID);
		}
	}
	
	public class RingBGFormat : RingFormat
	{
		public override Entry CreateRing()
		{
			return null;
		}

		public int CountRings(LayoutData layout, MultiFileIndexer<Block> Blocks, IEnumerable<ObjectEntry> objects)
		{
			int count = 0;
			int[] blkRings = new int[Blocks.Count];

			for (int blk = 0; blk < Blocks.Count; blk++)
			{
				blkRings[blk] = 0;
				for (int ty = 0; ty < 4; ty ++)
					for (int tx = 0; tx < 4; tx ++)
					{
						if (Blocks[blk].Tiles[tx, ty].Tile == 0x0001)
							blkRings[blk]++;
					}
			}
			for (int x = 0; x < layout.BGLayout.GetUpperBound(0); x ++)
			{
				for (int y = 0; y < layout.BGLayout.GetUpperBound(1); y ++)
				{
					count += blkRings[layout.BGLayout[x, y]];
					count += blkRings[layout.FGLayout[x, y]];
				}
			}

			if (objects != null)
			{
				// TODO: make this not hardcoded
				// Sky Chase Ring
				count += objects.Count(a => a.ID == 0x67);
				// Ring Item
				count += objects.Count(a => a.ID == 0x0C && a.SubType == 0x02) * 10;
			}
			return count;
		}
	}
}
