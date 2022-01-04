import NavigationItem from "./NavigationItem";
import NavigationTabContent from "../Navigation/NavigationTabContent";

import React, { Component } from "react";
import { connect } from "react-redux";

class Navigation extends Component {
  //mapState currentNavigationItem

  render() {
    const currentTab = this.props.currentTab;

    return (
      <section data-section-name="tabs">
        <div class="tabs-wrap">
          <div class="tabs__buttons">
            <ul class="tabs">
              <NavigationItem
                title="Gde želite da se premestite?"
                tabNumber={1}
                currentTab={currentTab}
              />
            </ul>
          </div>
          <NavigationTabContent currentTab={currentTab} />
        </div>
      </section>
    );
  }
}

const mapStateToProps = state => {
  return {
    currentTab: state.currentNavTab
  };
};

export default connect(mapStateToProps)(Navigation);
