﻿using System.Text;
using ParadoxPower.CSharp;
using ParadoxPower.Process;

namespace Moder.Core.Parser;

public class TextParser
{
	public string FilePath { get; }

	public bool IsSuccess { get; }

	public bool IsFailure => !IsSuccess;

	private readonly ParserError? _error;

	private readonly Node? _node;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="filePath"></param>
	/// <exception cref="FileNotFoundException">如果文件不存在</exception>
	/// <exception cref="IOException"></exception>
	public TextParser(string filePath)
	{
		FilePath = File.Exists(filePath) ? filePath : throw new FileNotFoundException($"找不到文件: {filePath}", filePath);
		var fileName = Path.GetFileName(filePath);
		var result = Parsers.ParseScriptFile(fileName, File.ReadAllText(filePath));
		IsSuccess = result.IsSuccess;
		if (IsFailure)
		{
			_error = result.GetError();
			return;
		}

		_node = Parsers.ProcessStatements(fileName, filePath, result.GetResult());
	}

	public static bool TryParse(string filePath, out Node node, out ParserError error)
	{
		var parser = new TextParser(filePath);
		if (parser.IsFailure)
		{
			node = null!;
			error = parser._error!;
			return false;
		}

		node = parser._node!;
		error = null!;
		return true;
	}

	static TextParser()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	public Node GetResult()
	{
		return _node ?? throw new InvalidOperationException($"文件解析失败, 无法返回解析结果, 文件路径: {FilePath}.");
	}

	public ParserError GetError()
	{
		return _error ?? throw new InvalidOperationException();
	}
}