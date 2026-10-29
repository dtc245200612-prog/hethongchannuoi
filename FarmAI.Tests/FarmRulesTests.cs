using FarmAI.Models;
using Xunit;

namespace FarmAI.Tests;

public class FarmRulesTests
{
    [Theory]
    [InlineData(100,20,120)]
    [InlineData(100,-10,90)]
    [InlineData(100,-100,0)]
    public void MovementMustConserveBalance(int old,int delta,int expected)
        => Assert.Equal(expected,FarmRules.Remaining(old,delta));
    [Fact]public void NegativeBalanceThrows()=>Assert.Throws<ArgumentException>(()=>FarmRules.Remaining(5,-6));
    [Fact]public void OverflowThrows()=>Assert.Throws<ArgumentException>(()=>FarmRules.Remaining(int.MaxValue,1));
    [Fact]public void PricePerHead()=>Assert.Equal(6000000m,FarmRules.LineTotal("HEAD",20,null,300000m));
    [Fact]public void PriceByWeight()=>Assert.Equal(15000000m,FarmRules.LineTotal("KG",20,300m,50000m));
    [Fact]public void WeightRequiredForKg()=>Assert.Throws<ArgumentException>(()=>FarmRules.LineTotal("KG",2,null,50m));
    [Fact]public void UnsupportedBasisThrows()=>Assert.Throws<ArgumentException>(()=>FarmRules.LineTotal("MONEY",2,100m,10m));
    [Fact]public void MortalityIsTwoPointFivePercent()=>Assert.Equal(2.50m,FarmRules.MortalityPercent(3,100,20));
    [Fact]public void ZeroDenominatorIsUnknown()=>Assert.Null(FarmRules.MortalityPercent(0,0,0));
    [Fact]public void AdgIsHalfKgPerDay()
    {
        var prior=new GrowthRecord{SampleSize=10,WeightKg=200,Method="RANDOM",MeasuredAt=new DateTime(2026,9,1)};
        var now=new GrowthRecord{SampleSize=10,WeightKg=270,Method="RANDOM",MeasuredAt=new DateTime(2026,9,15)};
        Assert.Equal(.5m,FarmRules.Adg(prior,now));
    }
    [Fact]public void DifferentMethodsInvalidateAdg()
    {
        var x=new GrowthRecord{SampleSize=10,WeightKg=200,Method="RANDOM",MeasuredAt=new DateTime(2026,9,1)};
        var y=new GrowthRecord{SampleSize=10,WeightKg=270,Method="FULL",MeasuredAt=new DateTime(2026,9,15)};
        Assert.Null(FarmRules.Adg(x,y));
    }
    [Fact]public void SameTimeInvalidatesAdg()
    {
        var at=new DateTime(2026,9,1);
        var x=new GrowthRecord{SampleSize=10,WeightKg=200,MeasuredAt=at};
        var y=new GrowthRecord{SampleSize=10,WeightKg=270,MeasuredAt=at};
        Assert.Null(FarmRules.Adg(x,y));
    }
}
