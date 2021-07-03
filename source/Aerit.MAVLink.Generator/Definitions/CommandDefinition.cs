using System;
using System.Collections.Generic;

namespace Aerit.MAVLink.Generator
{
	using static Utils;

	public record CommandDefinition(
		string Name,
		string? Description,
		string? Value,
		List<CommandParamDefinition> Params,
		DeprecatedDefinition? Deprecated)
	{
		public static CommandDefinition Create(EntryDefinition entry)
		{
			var @params = new List<CommandParamDefinition>();

			if (entry.Params is not null)
			{
				var index = 0;

				foreach (var param in entry.Params)
				{
					index++;

					if (param is null || param.Label is null)
					{
						continue;
					}

					var type = string.Empty;
					CommandParamValidationDefinition? validation = default;

					if (param.Enum is not null)
					{
						type = CamelCase(param.Enum);
					}
					else if (param.Increment is not null)
					{
						if (!uint.TryParse(param.Increment, out var increment))
						{
							throw new NotSupportedException();
						}

						if (param.MinValue is not null)
						{
							if (!long.TryParse(param.MinValue, out var min))
							{
								throw new NotSupportedException();
							}

							if (min == 0)
							{
								if (param.MaxValue is not null)
								{
									if (!long.TryParse(param.MaxValue, out var max))
									{
										throw new NotSupportedException();
									}

									if (max == 1)
									{
										type = "bool";
									}
									else
									{
										if (max < byte.MaxValue)
										{
											type = "byte";

											validation = new(null, max, increment);
										}
										else if (max == byte.MaxValue)
										{
											type = "byte";

											if (increment != 1)
											{
												validation = new(null, null, increment);
											}
										}
										else if (max < ushort.MaxValue)
										{
											type = "ushort";

											validation = new(null, max, increment);
										}
										else if (max == ushort.MaxValue)
										{
											type = "ushort";

											if (increment != 1)
											{
												validation = new(null, null, increment);
											}
										}
										else if (max < uint.MaxValue)
										{
											type = "uint";

											validation = new(null, max, increment);

										}
										else if (max == uint.MaxValue)
										{
											type = "uint";

											if (increment != 1)
											{
												validation = new(null, null, increment);
											}
										}
										else
										{
											throw new NotImplementedException();
										}
									}
								}
								else
								{
									type = "uint";

									validation = new(min, null, increment);
								}
							}
							else if (min > 0)
							{
								if (param.MaxValue is not null)
								{
									if (!long.TryParse(param.MaxValue, out var max))
									{
										throw new NotImplementedException();
									}

									if (max < byte.MaxValue)
									{
										type = "byte";

										validation = new(min, max, increment);
									}
									else if (max == byte.MaxValue)
									{
										type = "byte";

										validation = new(min, null, increment);
									}
									else if (max < ushort.MaxValue)
									{
										type = "ushort";

										validation = new(min, max, increment);
									}
									else if (max == ushort.MaxValue)
									{
										type = "ushort";

										validation = new(min, null, increment);
									}
									else if (max < uint.MaxValue)
									{
										type = "uint";

										validation = new(min, max, increment);
									}
									else if (max == uint.MaxValue)
									{
										type = "uint";

										validation = new(min, null, increment);
									}
									else
									{
										throw new NotImplementedException();
									}
								}
								else
								{
									type = "uint";

									validation = new(min, null, increment);
								}
							}
							else
							{
								if (param.MaxValue is not null)
								{
									if (!long.TryParse(param.MaxValue, out var max))
									{
										throw new NotImplementedException();
									}

									if (min >= sbyte.MinValue && max <= sbyte.MaxValue)
									{
										type = "sbyte";

										if (min == sbyte.MinValue)
										{
											if (max == sbyte.MaxValue)
											{
												if (increment != 1)
												{
													validation = new(null, null, increment);
												}
											}
											else
											{
												validation = new(null, max, increment);
											}
										}
										else
										{
											if (max == sbyte.MaxValue)
											{
												validation = new(min, null, increment);
											}
											else
											{
												validation = new(min, max, increment);
											}
										}
									}
									else if (min >= short.MinValue && max <= short.MaxValue)
									{
										type = "short";

										if (min == short.MinValue)
										{
											if (max == short.MaxValue)
											{
												if (increment != 1)
												{
													validation = new(null, null, increment);
												}
											}
											else
											{
												validation = new(null, max, increment);
											}
										}
										else
										{
											if (max == short.MaxValue)
											{
												validation = new(min, null, increment);
											}
											else
											{
												validation = new(min, max, increment);
											}
										}
									}
									else if (min >= int.MinValue && max <= int.MaxValue)
									{
										type = "int";

										if (min == int.MinValue)
										{
											if (max == int.MaxValue)
											{
												if (increment != 1)
												{
													validation = new(null, null, increment);
												}
											}
											else
											{
												validation = new(null, max, increment);
											}
										}
										else
										{
											if (max == int.MaxValue)
											{
												validation = new(min, null, increment);
											}
											else
											{
												validation = new(min, max, increment);
											}
										}
									}
									else
									{
										throw new NotSupportedException();
									}
								}
								else
								{
									type = "int";

									validation = new(min, null, increment);
								}
							}
						}
						else
						{
							type = "int";

							if (param.MaxValue is not null)
							{
								if (!long.TryParse(param.MaxValue, out var max) || max > int.MaxValue)
								{
									throw new NotImplementedException();
								}

								validation = new(null, max, increment);
							}
						}
					}
					else
					{
						type = "float";

						if (param.MinValue is not null)
						{
							if (!long.TryParse(param.MinValue, out var min))
							{
								throw new NotImplementedException();
							}

							if (param.MaxValue is not null)
							{
								if (!long.TryParse(param.MaxValue, out var max))
								{
									throw new NotImplementedException();
								}

								validation = new(min, max, null);
							}
							else
							{
								validation = new(min, null, null);
							}
						}
						else
						{
							if (param.MaxValue is not null)
							{
								if (!long.TryParse(param.MaxValue, out var max))
								{
									throw new NotImplementedException();
								}

								validation = new(null, max, null);
							}
						}
					}

					@params.Add(new(
						index,
						type,
						param.Default == "NaN",
						param.Label.Replace(" ", string.Empty),
						validation,
						param.Description,
						param.Units
					));
				}
			}

			return new(
				CamelCase(entry.Name, "MAV_CMD"),
				entry.Description,
				entry.Value,
				@params,
				entry.Deprecated
			);
		}
	}
}