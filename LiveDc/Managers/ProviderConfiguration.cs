using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LiveDc.Managers
{
    //<?xml version="1.0" encoding="utf-8" standalone="yes"?>
    //<Favorites>
    //    <Hubs>
    //      <Hub Name="Hub1" Server="dchub://dc.hub1.ru"/>
    //      <Hub Name="Hub2" Description="Большой Хаб" Server="dchub://dc.hub2.ru"/>
    //      <Hub Name="Hub3" Server="dchub://dc.hub3.ru:411"/>
    //    </Hubs>
    //    <Users/>
    //    <UserCommands/>
    //    <FavoriteDirs/>
    //    <LiveDC>
    //      <PortCheckUrl>http://livedc.april32.com/checkip.php</PortCheckUrl>
    //      <NetworkInterface>192.168</NetworkInterface>
    //    </LiveDC>
    //    <LocalIPRanges>
    //      <IPRange>10.0.0.0/8</IPRange>
    //      <IPRange>10.0.0.0-10.207.255.255</IPRange>
    //    </LocalIPRanges>
    //</Favorites>


    [Serializable]
    [XmlRoot("Favorites")]
    public struct ProviderConfiguration
    {
        [XmlElement("LiveDC")]
        public LiveDCConfig LiveDcConfig { get; set; }

        [XmlArrayItem("Hub")]
        public List<HubInfo> Hubs { get; set; }

        [XmlArrayItem("IPRange")]
        public List<string> LocalIPRanges { get; set; }
    }

    [Serializable]
    public struct HubInfo
    {
        [XmlAttributeAttribute("Name")]
        public string Name { get; set; }

        [XmlAttributeAttribute("Server")]
        public string Address { get; set; }

        [XmlAttributeAttribute("Description")]
        public string Description { get; set; }
    }

    [Serializable]
    public struct LiveDCConfig
    {
        public string PortCheckUrl { get; set; }

        public string NetworkInterface { get; set; }
    }
}
